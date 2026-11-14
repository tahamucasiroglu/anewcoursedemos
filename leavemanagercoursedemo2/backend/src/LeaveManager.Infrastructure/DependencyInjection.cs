using LeaveManager.Application.Abstractions;
using LeaveManager.Infrastructure.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppVersionProvider, AssemblyVersionProvider>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
