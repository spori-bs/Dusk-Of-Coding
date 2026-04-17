using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using DuskOfCoding.Infrastructure.Messaging;
using DuskOfCoding.Infrastructure.Configuration;
using DuskOfCoding.TutorWorker.Prompts;
using DuskOfCoding.TutorWorker.Resilience;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Domain.Enums;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace DuskOfCoding.TutorWorker;

/// <summary>
/// Background service that consumes submissions from RabbitMQ,
/// performs execution and AI evaluation, then provides Socratic tutoring.
/// </summary>
public sealed class TutorWorkerService : BackgroundService
{
    private readonly RabbitMQService _rabbitMqService;
    private readonly IChatClient _chatClient;
    private readonly ResiliencePipelineProvider<string> _resilienceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LlmProviderOptions _options;
    private readonly ILogger<TutorWorkerService> _logger;

    public TutorWorkerService(
        RabbitMQService rabbitMqService,
        IChatClient chatClient,
        ResiliencePipelineProvider<string> resilienceProvider,
        IServiceScopeFactory scopeFactory,
        IOptions<LlmProviderOptions> options,
        ILogger<TutorWorkerService> logger)
    {
        _rabbitMqService = rabbitMqService;
        _chatClient = chatClient;
        _resilienceProvider = resilienceProvider;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TutorWorker starting, subscribing to {Queue}...", RabbitMQTopology.TutorInteractionsQueue);

        var channel = await _rabbitMqService.ConsumeAsync<SubmissionMessage>(
            RabbitMQTopology.TutorInteractionsQueue,
            HandleSubmissionAsync,
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
            await channel.DisposeAsync();
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
            
            // 2. Roslyn Syntax Check (as previously in WebApi)
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

                // 4. Build structured feedback directly from execution result.
                //    The Socratic Tutor (step 7) is the sole LLM call — no duplicate AI review here.
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

            // 5. Save Results to DB
            submission.CompletedAt = DateTime.UtcNow;
            submission.Feedback ??= new FeedbackRecord { SubmissionId = submission.Id };
            submission.Feedback.IsSuccess = feedback.IsSuccess;
            submission.Feedback.Summary = feedback.Summary;
            submission.Feedback.AiReviewRemarks = feedback.AiReviewRemarks;
            submission.Feedback.SetCompilationMessages(feedback.CompilationMessages?.ToList() ?? new List<string>());
            submission.Feedback.SetTestMessages(feedback.TestMessages?.ToList() ?? new List<string>());

            await submissionRepo.UpdateAsync(submission, ct);

            // 6. Notify UI via SignalR
            await NotifyEvaluationComplete(message.SubmissionId, correlationId, feedback, ct);

            // 7. Follow up with the Socratic Tutor logic
            await HandleSocraticTutoring(message, correlationId, ct);
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

    private async Task NotifyEvaluationComplete(Guid submissionId, Guid correlationId, Feedback feedback, CancellationToken ct)
    {
        var response = new TutorResponseMessage
        {
            CorrelationId = correlationId,
            SubmissionId = submissionId,
            ResponseType = "evaluation",
            Content = feedback.Summary // This will be used by UI to update state
        };

        await _rabbitMqService.PublishAsync(
            RabbitMQTopology.ResponseExchange,
            RabbitMQTopology.ResponseRoutingKey,
            response,
            correlationId,
            ct);
    }

    private async Task HandleSocraticTutoring(SubmissionMessage message, Guid correlationId, CancellationToken ct)
    {
        var pipeline = _resilienceProvider.GetPipeline(LlmResilienceRegistration.PipelineName);

        try
        {
            var tutorResponse = await pipeline.ExecuteAsync(async token =>
            {
                return await InvokeSocraticTutorAsync(message, token);
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
             _logger.LogError(ex, "Socratic Tutor failed for submission {SubmissionId}", message.SubmissionId);
             // Fallback logic could go here
        }
    }

    private const int MaxSourceCodeLength = 50_000;

    private async Task<string> InvokeSocraticTutorAsync(SubmissionMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.SourceCode))
            return "⚠️ No source code was provided.";

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, SocraticTutorPrompt.GetSystemPrompt(message.PreferredLanguage)),
            new(ChatRole.User, $"""
                Please review my code submission:
                ```csharp
                {message.SourceCode}
                ```
                Language: {message.Language}
                """)
        };

        var chatOptions = new ChatOptions
        {
            Tools = [.. _chatClient.GetService<IList<AITool>>() ?? []],
            MaxOutputTokens = 2048
        };

        var response = await _chatClient.GetResponseAsync(chatMessages, chatOptions, ct);
        return response.Text ?? "I wasn't able to generate feedback.";
    }
}
