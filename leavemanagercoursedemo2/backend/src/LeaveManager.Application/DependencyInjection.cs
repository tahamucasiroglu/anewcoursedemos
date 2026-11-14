using LeaveManager.Application.Health;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetSystemHealth>();
        return services;
    }
}
