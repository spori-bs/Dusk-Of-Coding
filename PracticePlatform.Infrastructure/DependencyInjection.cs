using Microsoft.Extensions.DependencyInjection;
using PracticePlatform.Domain.Interfaces;
using PracticePlatform.Infrastructure.Services;

namespace PracticePlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register the Execution Engine with a typed HttpClient
        // Uses the Aspire service discovery name "executionapi" configured in AppHost.
        services.AddHttpClient<ICodeExecutionEngine, HttpCodeExecutionEngine>(client => 
        {
            client.BaseAddress = new Uri("http://executionapi");
        });

        return services;
    }
}
