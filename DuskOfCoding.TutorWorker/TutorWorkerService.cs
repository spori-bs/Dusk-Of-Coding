using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using DuskOfCoding.Infrastructure.Messaging;
using DuskOfCoding.Infrastructure.Configuration;
using DuskOfCoding.TutorWorker.Prompts;
using DuskOfCoding.TutorWorker.Resilience;
using DuskOfCoding.TutorWorker.McpTools;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Domain.Enums;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace DuskOfCoding.TutorWorker;

/// <summary>
/// Background service that consumes submissions & interactive hint requests from RabbitMQ,
/// performs Roslyn execution & Semantic Kernel Agent evaluation with tool calling.
/// </summary>
public sealed class TutorWorkerService : BackgroundService
{
    private readonly RabbitMQService _rabbitMqService;
    private readonly ResiliencePipelineProvider<string> _resilienceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LlmProviderOptions _options;
    private readonly ILogger<TutorWorkerService> _logger;

    public TutorWorkerService(
        RabbitMQService rabbitMqService,
        ResiliencePipelineProvider<string> resilienceProvider,
        IServiceScopeFactory scopeFactory,
        IOptions<LlmProviderOptions> options,
        ILogger<TutorWorkerService> logger)
    {
        _rabbitMqService = rabbitMqService;
        _resilienceProvider = resilienceProvider;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TutorWorker starting, subscribing to queues...");

        var subChannel = await _rabbitMqService.ConsumeAsync<SubmissionMessage>(
            RabbitMQTopology.TutorInteractionsQueue,
            HandleSubmissionAsync,
            stoppingToken);

        var hintChannel = await _rabbitMqService.ConsumeAsync<InteractiveHintRequestMessage>(
            RabbitMQTopology.InteractiveTutorQueue,
            HandleInteractiveHintAsync,
            stoppingToken);

        _logger.LogInformation("TutorWorker is now consuming messages (LLM provider: {Provider}, model: {Model})",
            _options.Provider, _options.ModelId);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TutorWorker shutting down...");
        }
        finally
        {
            await subChannel.DisposeAsync();
            await hintChannel.DisposeAsync();
        }
    }

    private async Task HandleSubmissionAsync(SubmissionMessage message, Guid correlationId, CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing submission {SubmissionId} with CorrelationId {CorrelationId}",
            message.SubmissionId, correlationId);

        using var scope = _scopeFactory.CreateScope();
        var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var submissionRepo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
        
        var submission = await submissionRepo.GetByIdAsync(message.SubmissionId, ct);
        if (submission == null)
        {
            _logger.LogError("Submission {SubmissionId} not found in database.", message.SubmissionId);
            return;
        }

        var task = await taskRepo.GetByIdAsync(message.TaskId, ct);
        if (task == null)
        {
            _logger.LogError("Task {TaskId} not found for submission {SubmissionId}.", message.TaskId, message.SubmissionId);
            return;
        }

        try
        {
            // 1. Initial State: Executing
            submission.Status = SubmissionStatus.Executing;
            await submissionRepo.UpdateAsync(submission, ct);

            Feedback feedback;
            
            // 2. Roslyn Syntax Check
            var syntaxTree = CSharpSyntaxTree.ParseText(message.SourceCode);
            var syntaxDiagnostics = syntaxTree.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => $"Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}")
                .ToList();

            if (syntaxDiagnostics.Any())
            {
                submission.Status = SubmissionStatus.CompilationFailed;
                feedback = new Feedback
                {
                    IsSuccess = false,
                    Summary = "Compilation Failed",
                    CompilationMessages = syntaxDiagnostics,
                    AiReviewRemarks = "The submission contains syntax errors. Please fix them before attempting execution."
                };
            }
            else
            {
                // 3. Resolve Execution Engine and Execute
                var targetLanguage = nameof(ProgrammingLanguage.CSharp);
                var executionEngine = scope.ServiceProvider.GetRequiredKeyedService<ICodeExecutionEngine>(targetLanguage);
                
                var executionResult = await executionEngine.ExecuteAsync(task, submission, ct);

                bool isSuccess = executionResult.CompilationSucceeded && executionResult.Tests.All(t => t.Passed);
                bool isHu = message.PreferredLanguage?.StartsWith("hu", StringComparison.OrdinalIgnoreCase) == true;
                feedback = new Feedback
                {
                    IsSuccess = isSuccess,
                    Summary = executionResult.CompilationSucceeded
                        ? (isHu ? "A futtatás befejeződött." : "Execution completed.")
                        : (isHu ? "A fordítás sikertelen." : "Compilation failed."),
                    CompilationMessages = executionResult.CompilationErrors,
                    TestMessages = executionResult.Tests
                        .Select(t => $"{t.Name}: {(t.Passed ? "Passed" : "Failed")} {t.Message}")
                        .ToList()
                };
                submission.Status = isSuccess ? SubmissionStatus.Success : SubmissionStatus.TestsFailed;
            }

            // 4. Save Results to DB
            submission.CompletedAt = DateTime.UtcNow;
            submission.Feedback ??= new FeedbackRecord { SubmissionId = submission.Id };
            submission.Feedback.IsSuccess = feedback.IsSuccess;
            submission.Feedback.Summary = feedback.Summary;
            submission.Feedback.AiReviewRemarks = feedback.AiReviewRemarks;
            submission.Feedback.SetCompilationMessages(feedback.CompilationMessages?.ToList() ?? new List<string>());
            submission.Feedback.SetTestMessages(feedback.TestMessages?.ToList() ?? new List<string>());

            await submissionRepo.UpdateAsync(submission, ct);

            // 5. Notify UI via SignalR
            await NotifyEvaluationComplete(message.SubmissionId, correlationId, feedback, ct);

            // 6. Follow up with Socratic Tutor Agent
            await HandleSocraticTutoring(message, feedback, correlationId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate submission {SubmissionId}", message.SubmissionId);
            submission.Status = SubmissionStatus.Error;
            await submissionRepo.UpdateAsync(submission, ct);
            
            await _rabbitMqService.PublishAsync(
                RabbitMQTopology.ResponseExchange,
                RabbitMQTopology.ResponseRoutingKey,
                new TutorResponseMessage
                {
                    CorrelationId = correlationId,
                    SubmissionId = message.SubmissionId,
                    ResponseType = "error",
                    Content = $"An error occurred during evaluation: {ex.Message}"
                }, correlationId, ct);
        }
    }

    private async Task HandleInteractiveHintAsync(InteractiveHintRequestMessage message, Guid correlationId, CancellationToken ct)
    {
        _logger.LogInformation("Processing Lightbulb interactive hint request for Task {TaskId}", message.TaskId);
        var pipeline = _resilienceProvider.GetPipeline(LlmResilienceRegistration.PipelineName);

        try
        {
            var tutorResponse = await pipeline.ExecuteAsync(async token =>
            {
                return await InvokeInteractiveHintAgentAsync(message, token);
            }, ct);

            var response = new TutorResponseMessage
            {
                CorrelationId = correlationId,
                SubmissionId = message.SubmissionId,
                ResponseType = "hint",
                Content = tutorResponse
            };

            await _rabbitMqService.PublishAsync(
                RabbitMQTopology.ResponseExchange,
                RabbitMQTopology.ResponseRoutingKey,
                response,
                correlationId,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Interactive Lightbulb hint failed for Task {TaskId}", message.TaskId);
        }
    }

    private async Task NotifyEvaluationComplete(Guid submissionId, Guid correlationId, Feedback feedback, CancellationToken ct)
    {
        var response = new TutorResponseMessage
        {
            CorrelationId = correlationId,
            SubmissionId = submissionId,
            ResponseType = "evaluation",
            Content = feedback.Summary
        };

        await _rabbitMqService.PublishAsync(
            RabbitMQTopology.ResponseExchange,
            RabbitMQTopology.ResponseRoutingKey,
            response,
            correlationId,
            ct);
    }

    private async Task HandleSocraticTutoring(SubmissionMessage message, Feedback feedback, Guid correlationId, CancellationToken ct)
    {
        var pipeline = _resilienceProvider.GetPipeline(LlmResilienceRegistration.PipelineName);

        try
        {
            var tutorResponse = await pipeline.ExecuteAsync(async token =>
            {
                return await InvokeSocraticTutorAgentAsync(message, feedback, token);
            }, ct);

            var response = new TutorResponseMessage
            {
                CorrelationId = correlationId,
                SubmissionId = message.SubmissionId,
                ResponseType = "feedback",
                Content = tutorResponse
            };

            await _rabbitMqService.PublishAsync(
                RabbitMQTopology.ResponseExchange,
                RabbitMQTopology.ResponseRoutingKey,
                response,
                correlationId,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Socratic Tutor Agent failed for submission {SubmissionId}", message.SubmissionId);
        }
    }

    private async Task<string> InvokeSocraticTutorAgentAsync(SubmissionMessage message, Feedback feedback, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.SourceCode))
            return "⚠️ No source code was provided.";

        using var scope = _scopeFactory.CreateScope();
        var kernel = scope.ServiceProvider.GetRequiredService<Kernel>();
        kernel.Plugins.AddFromType<AnalyzeCodeTool>();
        kernel.Plugins.AddFromType<ExecuteCustomTestTool>();

        var testOutput = feedback?.TestMessages?.Any() == true
            ? $"\n\nUnit Test Output:\n{string.Join("\n", feedback.TestMessages)}"
            : string.Empty;

        var compilationsOutput = feedback?.CompilationMessages?.Any() == true
            ? $"\n\nCompilation Errors:\n{string.Join("\n", feedback.CompilationMessages)}"
            : string.Empty;

        var agent = new ChatCompletionAgent
        {
            Name = "SocraticTutor",
            Instructions = SocraticTutorPrompt.GetSystemPrompt(message.PreferredLanguage),
            Kernel = kernel,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };

        var prompt = $"""
            Please review my code submission:
            ```csharp
            {message.SourceCode}
            ```
            Language: {message.Language}
            {compilationsOutput}
            {testOutput}
            """;

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var responseText = string.Empty;
        await foreach (var item in agent.InvokeAsync(chatHistory, cancellationToken: ct))
        {
            if (!string.IsNullOrWhiteSpace(item.Message.Content))
            {
                responseText += item.Message.Content;
            }
        }

        return string.IsNullOrWhiteSpace(responseText) ? "I wasn't able to generate feedback." : responseText;
    }

    private async Task<string> InvokeInteractiveHintAgentAsync(InteractiveHintRequestMessage message, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var kernel = scope.ServiceProvider.GetRequiredService<Kernel>();
        kernel.Plugins.AddFromType<AnalyzeCodeTool>();

        var agent = new ChatCompletionAgent
        {
            Name = "InteractiveSocraticAdvisor",
            Instructions = SocraticTutorPrompt.GetInteractiveHintPrompt(message.PreferredLanguage),
            Kernel = kernel,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };

        var userPrompt = string.IsNullOrWhiteSpace(message.UserQuestion)
            ? $"Student clicked Lightbulb 💡 for assistance on draft code:\n```csharp\n{message.DraftSourceCode}\n```"
            : $"Student asked: {message.UserQuestion}\nDraft code:\n```csharp\n{message.DraftSourceCode}\n```";

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(userPrompt);

        var responseText = string.Empty;
        await foreach (var item in agent.InvokeAsync(chatHistory, cancellationToken: ct))
        {
            if (!string.IsNullOrWhiteSpace(item.Message.Content))
            {
                responseText += item.Message.Content;
            }
        }

        return string.IsNullOrWhiteSpace(responseText) ? "Keep thinking about your algorithm and code structure!" : responseText;
    }
}
