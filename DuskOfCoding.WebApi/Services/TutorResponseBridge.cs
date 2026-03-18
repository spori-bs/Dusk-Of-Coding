using Microsoft.AspNetCore.SignalR;
using DuskOfCoding.Infrastructure.Messaging;
using DuskOfCoding.WebApi.Hubs;

namespace DuskOfCoding.WebApi.Services;

/// <summary>
/// Background service that consumes TutorResponseMessages from RabbitMQ
/// and forwards them to connected SignalR clients via the TutorHub.
/// This is the bridge between the async messaging layer and the real-time UI.
/// </summary>
public sealed class TutorResponseBridge : BackgroundService
{
    private readonly RabbitMQService _rabbitMqService;
    private readonly IHubContext<TutorHub> _hubContext;
    private readonly ILogger<TutorResponseBridge> _logger;

    public TutorResponseBridge(
        RabbitMQService rabbitMqService,
        IHubContext<TutorHub> hubContext,
        ILogger<TutorResponseBridge> logger)
    {
        _rabbitMqService = rabbitMqService;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TutorResponseBridge starting, subscribing to {Queue}...",
            RabbitMQTopology.TutorResponsesQueue);

        var channel = await _rabbitMqService.ConsumeAsync<TutorResponseMessage>(
            RabbitMQTopology.TutorResponsesQueue,
            HandleResponseAsync,
            stoppingToken);

        _logger.LogInformation("TutorResponseBridge is now consuming tutor responses");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TutorResponseBridge shutting down...");
        }
        finally
        {
            await channel.DisposeAsync();
        }
    }

    private async Task HandleResponseAsync(TutorResponseMessage message, Guid correlationId, CancellationToken ct)
    {
        _logger.LogInformation(
            "Forwarding tutor response for submission {SubmissionId} to SignalR (type: {ResponseType})",
            message.SubmissionId, message.ResponseType);

        // Send to the SignalR group keyed by submission ID
        await _hubContext.Clients
            .Group(message.SubmissionId.ToString())
            .SendAsync("ReceiveTutorResponse", new
            {
                message.SubmissionId,
                message.ResponseType,
                message.Content,
                message.RawDiagnostics,
                CorrelationId = correlationId.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            }, ct);
    }
}
