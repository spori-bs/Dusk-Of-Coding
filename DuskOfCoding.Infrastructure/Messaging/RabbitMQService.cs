using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DuskOfCoding.Infrastructure.Messaging;

/// <summary>
/// Manages the RabbitMQ connection, topology declaration, publishing, and consuming.
/// Uses Polly for resilient connection and publish operations.
/// </summary>
public sealed class RabbitMQService : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly ResiliencePipeline _publishPipeline;
    private IChannel? _publishChannel;

    public RabbitMQService(IConnection connection, ILogger<RabbitMQService> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Polly Resilience Pipeline for publish operations
        _publishPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    ex is RabbitMQ.Client.Exceptions.AlreadyClosedException ||
                    ex is System.IO.IOException ||
                    ex is TimeoutException),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "RabbitMQ publish retry #{AttemptNumber} after {Delay}ms. Reason: {Reason}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "unknown");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }

    /// <summary>
    /// Declares the full exchange/queue topology. Call once at startup.
    /// </summary>
    public async Task DeclareTopologyAsync(CancellationToken ct = default)
    {
        await using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        _logger.LogInformation("Declaring RabbitMQ topology...");

        // Declare exchanges
        await channel.ExchangeDeclareAsync(
            exchange: RabbitMQTopology.SubmissionExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: RabbitMQTopology.ResponseExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: RabbitMQTopology.TestGenerationExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        // Declare queues
        await channel.QueueDeclareAsync(
            queue: RabbitMQTopology.TutorInteractionsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: RabbitMQTopology.TutorResponsesQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: RabbitMQTopology.TestGenerationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: RabbitMQTopology.TestGenerationResponseQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: RabbitMQTopology.InteractiveTutorQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        // Bind queues to exchanges
        await channel.QueueBindAsync(
            queue: RabbitMQTopology.TutorInteractionsQueue,
            exchange: RabbitMQTopology.SubmissionExchange,
            routingKey: RabbitMQTopology.SubmissionRoutingKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: RabbitMQTopology.TutorResponsesQueue,
            exchange: RabbitMQTopology.ResponseExchange,
            routingKey: RabbitMQTopology.ResponseRoutingKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: RabbitMQTopology.TestGenerationQueue,
            exchange: RabbitMQTopology.TestGenerationExchange,
            routingKey: RabbitMQTopology.TestGenerationRoutingKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: RabbitMQTopology.TestGenerationResponseQueue,
            exchange: RabbitMQTopology.ResponseExchange,
            routingKey: RabbitMQTopology.TestGenerationResponseRoutingKey,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: RabbitMQTopology.InteractiveTutorQueue,
            exchange: RabbitMQTopology.SubmissionExchange,
            routingKey: RabbitMQTopology.InteractiveTutorRoutingKey,
            cancellationToken: ct);

        _logger.LogInformation("RabbitMQ topology declared successfully");
    }

    /// <summary>
    /// Publishes a message to the specified exchange with CorrelationId tracking.
    /// Wrapped in Polly resilience pipeline for transient failure handling.
    /// </summary>
    public async Task PublishAsync<T>(
        string exchange,
        string routingKey,
        T message,
        Guid correlationId,
        CancellationToken ct = default) where T : class
    {
        await _publishPipeline.ExecuteAsync(async token =>
        {
            _publishChannel ??= await _connection.CreateChannelAsync(cancellationToken: token);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                Persistent = true,
                CorrelationId = correlationId.ToString(),
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _publishChannel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: token);

            _logger.LogDebug(
                "Published message to {Exchange}/{RoutingKey} with CorrelationId {CorrelationId}",
                exchange, routingKey, correlationId);
        }, ct);
    }

    /// <summary>
    /// Starts consuming messages from the specified queue.
    /// Returns the channel so the caller can manage its lifetime.
    /// </summary>
    public async Task<IChannel> ConsumeAsync<T>(
        string queue,
        Func<T, Guid, CancellationToken, Task> handler,
        CancellationToken ct = default) where T : class
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        // Prefetch 1 message at a time for fair dispatch
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<T>(ea.Body.Span);
                if (message is null)
                {
                    _logger.LogWarning("Failed to deserialize message from {Queue}", queue);
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
                    return;
                }

                var correlationId = Guid.TryParse(ea.BasicProperties.CorrelationId, out var cid)
                    ? cid
                    : Guid.Empty;

                await handler(message, correlationId, ct);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);

                _logger.LogDebug(
                    "Processed and acknowledged message from {Queue} with CorrelationId {CorrelationId}",
                    queue, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {Queue}", queue);
                // Requeue on failure so it can be retried
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation("Started consuming from queue {Queue}", queue);

        return channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_publishChannel is not null)
        {
            await _publishChannel.CloseAsync();
            _publishChannel.Dispose();
        }
    }
}
