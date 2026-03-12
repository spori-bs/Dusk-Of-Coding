using PracticePlatform.Infrastructure.Messaging;

namespace PracticePlatform.TutorWorker;

/// <summary>
/// Background service that consumes submissions from RabbitMQ,
/// processes them through MCP tools (Roslyn analysis, code execution),
/// and publishes tutor responses back to the response exchange.
/// </summary>
public sealed class TutorWorkerService : BackgroundService
{
    private readonly RabbitMQService _rabbitMqService;
    private readonly ILogger<TutorWorkerService> _logger;

    public TutorWorkerService(RabbitMQService rabbitMqService, ILogger<TutorWorkerService> logger)
    {
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TutorWorker starting, subscribing to {Queue}...", RabbitMQTopology.TutorInteractionsQueue);

        // Consume from the TutorInteractionsQueue
        var channel = await _rabbitMqService.ConsumeAsync<SubmissionMessage>(
            RabbitMQTopology.TutorInteractionsQueue,
            HandleSubmissionAsync,
            stoppingToken);

        _logger.LogInformation("TutorWorker is now consuming messages");

        // Keep the service alive until cancellation
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

        try
        {
            // Phase 2: Basic flow — analyze code with Roslyn, then respond
            // Phase 3 will add the LLM Socratic Tutor loop here
            var analysisResult = McpTools.AnalyzeCodeTool.AnalyzeCode(message.SourceCode, ct);

            string responseType;
            string content;

            if (analysisResult.StartsWith("✅"))
            {
                responseType = "feedback";
                content = $"Your code passes syntax analysis.\n\n{analysisResult}\n\n" +
                          "The Socratic Tutor will provide deeper guidance once the LLM integration is complete (Phase 3).";
            }
            else
            {
                responseType = "hint";
                content = $"I found some issues in your code. Let's work through them:\n\n{analysisResult}\n\n" +
                          "Try fixing these syntax errors first, then resubmit.";
            }

            var response = new TutorResponseMessage
            {
                CorrelationId = correlationId,
                SubmissionId = message.SubmissionId,
                ResponseType = responseType,
                Content = content,
                RawDiagnostics = analysisResult
            };

            await _rabbitMqService.PublishAsync(
                RabbitMQTopology.ResponseExchange,
                RabbitMQTopology.ResponseRoutingKey,
                response,
                correlationId,
                ct);

            _logger.LogInformation(
                "Published tutor response for submission {SubmissionId} (type: {ResponseType})",
                message.SubmissionId, responseType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing submission {SubmissionId}", message.SubmissionId);

            // Always respond — no stuck requests
            var errorResponse = new TutorResponseMessage
            {
                CorrelationId = correlationId,
                SubmissionId = message.SubmissionId,
                ResponseType = "error",
                Content = "An unexpected error occurred while processing your submission. Please try again later."
            };

            await _rabbitMqService.PublishAsync(
                RabbitMQTopology.ResponseExchange,
                RabbitMQTopology.ResponseRoutingKey,
                errorResponse,
                correlationId,
                ct);
        }
    }
}
