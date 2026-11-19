using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator.DependencyInjection;

namespace OkulYonetim.Application;

/// <summary>
/// Application katmanının servis kaydı: nArchitecture Mediator (handler taraması) ve işlem log pipeline'ı
/// (<c>ILoggableRequest</c> işaretli istekler loglanır).
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMediator(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(typeof(ApplicationServiceRegistration).Assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
