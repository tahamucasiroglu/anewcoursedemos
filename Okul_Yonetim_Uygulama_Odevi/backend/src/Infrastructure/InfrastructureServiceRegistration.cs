using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Infrastructure.Logging;
using NArchitectureLogger = NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction.ILogger;

namespace OkulYonetim.Infrastructure;

/// <summary>
/// Infrastructure katmanının servis kaydı: nArchitecture log arayüzünün Serilog adaptörü. Dış servis
/// port uygulamaları (ödeme, SMS, AI…) ilgili özelliklerle buraya eklenir.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<NArchitectureLogger, SerilogLoggerAdapter>();
        return services;
    }
}
