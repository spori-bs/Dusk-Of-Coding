using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using DuskOfCoding.TutorWorker.Configuration;

namespace DuskOfCoding.TutorWorker.Resilience;

/// <summary>
/// Registers a named Polly ResiliencePipeline for LLM API calls.
/// Handles retries (HTTP 5xx, 429), timeout, and fallback.
/// </summary>
public static class LlmResilienceRegistration
{
    public const string PipelineName = "llm-pipeline";

    public static IServiceCollection AddLlmResilience(this IServiceCollection services)
    {
        services.AddResiliencePipeline(PipelineName, (builder, context) =>
        {
            var options = context.ServiceProvider
                .GetRequiredService<IOptions<LlmProviderOptions>>().Value;

            // 1. Overall timeout for the entire LLM thought process
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                OnTimeout = args =>
                {
                    var logger = context.ServiceProvider.GetRequiredService<ILogger<ResiliencePipelineBuilder>>();
                    logger.LogWarning("LLM call timed out after {Timeout}s", options.TimeoutSeconds);
                    return ValueTask.CompletedTask;
                }
            });

            // 2. Retry with exponential backoff for transient failures
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>(ex =>
                        // 5xx server errors or 429 rate limits
                        ex.StatusCode is System.Net.HttpStatusCode.InternalServerError or
                            System.Net.HttpStatusCode.BadGateway or
                            System.Net.HttpStatusCode.ServiceUnavailable or
                            System.Net.HttpStatusCode.GatewayTimeout or
                            System.Net.HttpStatusCode.TooManyRequests)
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>(),
                OnRetry = args =>
                {
                    var logger = context.ServiceProvider.GetRequiredService<ILogger<ResiliencePipelineBuilder>>();
                    logger.LogWarning(
                        "LLM retry #{AttemptNumber}/{MaxRetries} after {Delay}ms. Reason: {Reason}",
                        args.AttemptNumber,
                        options.MaxRetryAttempts,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "unknown");
                    return ValueTask.CompletedTask;
                }
            });
        });

        return services;
    }
}
