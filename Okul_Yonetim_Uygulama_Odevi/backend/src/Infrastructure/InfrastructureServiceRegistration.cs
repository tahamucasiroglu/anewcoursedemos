using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Email;
using OkulYonetim.Infrastructure.Logging;
using OkulYonetim.Infrastructure.Security;
using NArchitectureLogger = NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction.ILogger;

namespace OkulYonetim.Infrastructure;

/// <summary>
/// Infrastructure katmanının servis kaydı: nArchitecture log arayüzünün Serilog adaptörü, şifre özeti ve oturum
/// anahtarı servisi ("Jwt" ayarları), e-posta ("Email" ayarları; kuyruk + arka plan gönderici — spec 0006). Dış servis port uygulamaları (ödeme, SMS, AI…) ilgili özelliklerle buraya eklenir.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<NArchitectureLogger, SerilogLoggerAdapter>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();
        services.AddSingleton<ITotp, Totp>();
        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IAppLinks, AppLinks>();
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<BackgroundEmailOutbox>();
        services.AddSingleton<IEmailOutbox>(provider => provider.GetRequiredService<BackgroundEmailOutbox>());
        services.AddHostedService(provider => provider.GetRequiredService<BackgroundEmailOutbox>());
        return services;
    }
}
