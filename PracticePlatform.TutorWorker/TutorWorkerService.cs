using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using PracticePlatform.Infrastructure.Messaging;
using PracticePlatform.TutorWorker.Configuration;
using PracticePlatform.TutorWorker.Prompts;
using PracticePlatform.TutorWorker.Resilience;

namespace PracticePlatform.TutorWorker;

/// <summary>
/// Background service that consumes submissions from RabbitMQ,
/// processes them through the Socratic Tutor LLM with MCP tools,
/// and publishes tutor responses back to the response exchange.
/// </summary>
public sealed class TutorWorkerService : BackgroundService
{
    private readonly RabbitMQService _rabbitMqService;
    private readonly IChatClient _chatClient;
    private readonly ResiliencePipelineProvider<string> _resilienceProvider;
    private readonly LlmProviderOptions _options;
    private readonly ILogger<TutorWorkerService> _logger;

    public TutorWorkerService(
        RabbitMQService rabbitMqService,
        IChatClient chatClient,
        ResiliencePipelineProvider<string> resilienceProvider,
        IOptions<LlmProviderOptions> options,
        ILogger<TutorWorkerService> logger)
    {
        _rabbitMqService = rabbitMqService;
        _chatClient = chatClient;
        _resilienceProvider = resilienceProvider;
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

        var pipeline = _resilienceProvider.GetPipeline(LlmResilienceRegistration.PipelineName);

        try
        {
            // Try the LLM with Polly resilience
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

            _logger.LogInformation("Published tutor response for submission {SubmissionId}", message.SubmissionId);
        }
        catch (Exception ex)
        {
            // Fallback: LLM completely failed — send safety message + raw diagnostics
            _logger.LogError(ex, "LLM failed for submission {SubmissionId}, executing fallback", message.SubmissionId);

            var rawDiagnostics = McpTools.AnalyzeCodeTool.AnalyzeCode(message.SourceCode, ct);

            var fallbackResponse = new TutorResponseMessage
            {
                CorrelationId = correlationId,
                SubmissionId = message.SubmissionId,
                ResponseType = "safety",
                Content = _options.FallbackMessage,
                RawDiagnostics = rawDiagnostics
            };

            await _rabbitMqService.PublishAsync(
                RabbitMQTopology.ResponseExchange,
                RabbitMQTopology.ResponseRoutingKey,
                fallbackResponse,
                correlationId,
                ct);
        }
    }

    /// <summary>Maximum source code length accepted per submission (50 KB).</summary>
    private const int MaxSourceCodeLength = 50_000;

    private async Task<string> InvokeSocraticTutorAsync(SubmissionMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.SourceCode))
            return "⚠️ No source code was provided. Please submit your code and try again.";

        if (message.SourceCode.Length > MaxSourceCodeLength)
            return $"⚠️ Source code exceeds the maximum allowed length of {MaxSourceCodeLength:N0} characters. Please reduce the code size and try again.";

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, SocraticTutorPrompt.SystemPrompt),
            new(ChatRole.User, $"""
                Please review my code submission for the practice exercise.

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

        return response.Text ?? "I wasn't able to generate feedback for your submission. Please try again.";
    }
}
