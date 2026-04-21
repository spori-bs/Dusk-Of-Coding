using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DuskOfCoding.Application.Services;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Infrastructure.Messaging;
using DuskOfCoding.Infrastructure.Persistence;
using DuskOfCoding.Infrastructure.Repositories;
using DuskOfCoding.Infrastructure.Services;
using DuskOfCoding.Domain.Enums;

namespace DuskOfCoding.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string? connectionString = null)
    {
        // Register EF Core with MariaDB
        services.AddDbContext<AppDbContext>(options =>
        {
            var cs = connectionString ?? "Server=localhost;Database=dusk_db;User=root;Password=password;";
            try
            {
                options.UseMySql(cs, ServerVersion.AutoDetect(cs));
            }
            catch
            {
                options.UseMySql(cs, new MariaDbServerVersion(new Version(10, 11, 4)));
            }
        });

        // Register EF Core repositories
        services.AddScoped<ITaskRepository, EfTaskRepository>();
        services.AddScoped<ISubmissionRepository, EfSubmissionRepository>();
        services.AddScoped<IFeedbackRepository, EfFeedbackRepository>();
        services.AddScoped<ITelemetryService, TelemetryService>();
        services.AddConfigurableChatClient();
        services.AddScoped<IAIReviewService, AiReviewService>();

        // Register the Execution Engine with a typed HttpClient
        // Uses the Aspire service discovery name "executionapi" configured in AppHost.
        services.AddHttpClient("executionapi", client => 
        {
            client.BaseAddress = new Uri("http://executionapi");
        })
        .AddStandardResilienceHandler();

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


