using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticePlatform.Domain.Interfaces;
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
        services.AddSingleton<IAIReviewService, NoOpAIReviewService>();

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
}

