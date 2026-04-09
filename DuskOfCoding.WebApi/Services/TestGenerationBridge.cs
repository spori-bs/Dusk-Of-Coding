using Microsoft.AspNetCore.SignalR;
using DuskOfCoding.Infrastructure.Messaging;
using DuskOfCoding.WebApi.Hubs;

namespace DuskOfCoding.WebApi.Services;

/// <summary>
/// Background service that consumes TestGenerationResultMessages from RabbitMQ
/// and forwards them to the originating user via SignalR (user-targeted, not group-based).
/// Mirrors the TutorResponseBridge pattern for the test generation pipeline.
/// </summary>
public sealed class TestGenerationBridge : BackgroundService
{
    private readonly RabbitMQService _rabbitMqService;
    private readonly IHubContext<TutorHub> _hubContext;
    private readonly ILogger<TestGenerationBridge> _logger;

    public TestGenerationBridge(
        RabbitMQService rabbitMqService,
        IHubContext<TutorHub> hubContext,
        ILogger<TestGenerationBridge> logger)
    {
        _rabbitMqService = rabbitMqService;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TestGenerationBridge starting, subscribing to {Queue}...",
            RabbitMQTopology.TestGenerationResponseQueue);

        var channel = await _rabbitMqService.ConsumeAsync<TestGenerationResultMessage>(
            RabbitMQTopology.TestGenerationResponseQueue,
            HandleResultAsync,
            stoppingToken);

        _logger.LogInformation("TestGenerationBridge is now consuming test generation results");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TestGenerationBridge shutting down...");
        }
        finally
        {
            await channel.DisposeAsync();
        }
    }

    private async Task HandleResultAsync(TestGenerationResultMessage message, Guid correlationId, CancellationToken ct)
    {
        _logger.LogInformation(
            "Forwarding test generation result for Task {TaskId} to user {UserId} (success: {IsSuccess})",
            message.TaskId, message.UserId, message.IsSuccess);

        await _hubContext.Clients
            .User(message.UserId)
            .SendAsync("TestSuiteGenerationCompleted", message.TaskId, message.IsSuccess, message.MessageKey, ct);
    }
}
