using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PracticePlatform.Domain.Interfaces;
using PracticePlatform.Infrastructure.Messaging;
using PracticePlatform.Infrastructure.Persistence;
using PracticePlatform.Infrastructure.Repositories;
using PracticePlatform.Infrastructure.Services;
using PracticePlatform.Domain.Enums;

namespace PracticePlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string? connectionString = null)
    {
        // Register EF Core with SQLite
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString ?? "Data Source=practiceplatform.db"));

        // Register EF Core repositories
        services.AddScoped<ITaskRepository, EfTaskRepository>();
        services.AddScoped<ISubmissionRepository, EfSubmissionRepository>();
        services.AddSingleton<IAIReviewService, MockAIReviewService>();

        // Register the Execution Engine with a typed HttpClient
        // Uses the Aspire service discovery name "executionapi" configured in AppHost.
        services.AddHttpClient("executionapi", client => 
        {
            client.BaseAddress = new Uri("http://executionapi");
        });

        services.AddKeyedScoped<ICodeExecutionEngine, HttpCodeExecutionEngine>(nameof(ProgrammingLanguage.CSharp), (sp, key) => 
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new HttpCodeExecutionEngine(factory.CreateClient("executionapi"));
        });

        return services;
    }

    /// <summary>
    /// Registers RabbitMQ messaging infrastructure.
    /// Call this in addition to <see cref="AddInfrastructureServices"/> when RabbitMQ is available.
    /// </summary>
    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        // RabbitMQService wraps the IConnection provided by Aspire's AddRabbitMQClient
        services.AddSingleton<RabbitMQService>();

        // Declare topology (exchanges, queues, bindings) on startup
        services.AddHostedService<TopologyInitializer>();

        return services;
    }
}


