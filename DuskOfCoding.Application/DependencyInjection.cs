using Microsoft.Extensions.DependencyInjection;
using DuskOfCoding.Application.Services;

namespace DuskOfCoding.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ISubmissionService, SubmissionService>();

        return services;
    }
}
