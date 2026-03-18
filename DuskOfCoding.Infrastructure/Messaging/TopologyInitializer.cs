using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DuskOfCoding.Infrastructure.Messaging;

/// <summary>
/// Hosted service that declares the RabbitMQ topology on application startup.
/// Ensures exchanges, queues, and bindings exist before any messages are published or consumed.
/// </summary>
public sealed class TopologyInitializer : IHostedService
{
    private readonly RabbitMQService _rabbitMqService;
    private readonly ILogger<TopologyInitializer> _logger;

    public TopologyInitializer(RabbitMQService rabbitMqService, ILogger<TopologyInitializer> logger)
    {
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing RabbitMQ topology...");
        await _rabbitMqService.DeclareTopologyAsync(cancellationToken);
        _logger.LogInformation("RabbitMQ topology initialization complete");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
