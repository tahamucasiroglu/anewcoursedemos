using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Ai;
using OkulYonetim.Infrastructure.Email;
using OkulYonetim.Infrastructure.Logging;
using OkulYonetim.Infrastructure.Notifications;
using OkulYonetim.Infrastructure.Security;
using NArchitectureLogger = NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction.ILogger;

namespace OkulYonetim.Infrastructure;

/// <summary>
/// Infrastructure katmanının servis kaydı: nArchitecture log arayüzünün Serilog adaptörü, şifre özeti ve oturum
/// anahtarı servisi ("Jwt" ayarları), gizli değer ve kişisel veri şifreleyicileri (ayrı AES-GCM anahtarları — spec 0007, 0009), e-posta ("Email" ayarları; kuyruk + arka plan gönderici — spec 0006), bildirim göndericisi (spec 0011: gerçek push yok, yalnızca kimlikleri loglayan sahte gönderici), yerel yapay zekâ (spec 0012: "Ai" ayarları; adres boş ya da yerel değilse "kullanılamıyor" uygulaması, aksi halde Ollama ve ses servisi adaptörleri — tek iş sırasıyla). Dış servis port uygulamaları (ödeme, SMS, AI…) ilgili özelliklerle buraya eklenir.
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
        services.AddSingleton<IPersonalDataProtector, AesGcmPersonalDataProtector>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IAppLinks, AppLinks>();
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<IPushNotifier, LoggingPushNotifier>();
        services.AddSingleton<BackgroundEmailOutbox>();
        services.AddSingleton<IEmailOutbox>(provider => provider.GetRequiredService<BackgroundEmailOutbox>());
        services.AddHostedService(provider => provider.GetRequiredService<BackgroundEmailOutbox>());
        AddLocalAi(services, configuration);
        return services;
    }

    private static void AddLocalAi(IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(AiOptions.SectionName);
        services.Configure<AiOptions>(section);
        AiOptions ai = section.Get<AiOptions>() ?? new AiOptions();
        services.AddSingleton(new AiWorkGate(TimeSpan.FromSeconds(ai.QueueTimeoutSeconds)));

        string? textReason = Unavailable(ai.TextGenerator) ?? (string.IsNullOrWhiteSpace(ai.TextGenerator.Model) ? "not-configured" : null);
        if (textReason is not null)
        {
            services.AddSingleton<IAiTextGenerator>(new UnavailableTextGenerator(textReason));
        }
        else
        {
            services.AddHttpClient<IAiTextGenerator, OllamaTextGenerator>(client => Point(client, ai.TextGenerator)).ConfigurePrimaryHttpMessageHandler(NoRedirects);
        }

        if (Unavailable(ai.SpeechToText) is { } speechReason)
        {
            services.AddSingleton<ISpeechToText>(new UnavailableSpeechToText(speechReason));
        }
        else
        {
            services.AddHttpClient<ISpeechToText, WhisperServiceSpeechToText>(client => Point(client, ai.SpeechToText)).ConfigurePrimaryHttpMessageHandler(NoRedirects);
        }
    }

    /// <summary>Adres boşsa <c>not-configured</c>, yerel değilse <c>not-local</c>; kullanılabilirse null.</summary>
    private static string? Unavailable(AiEndpointOptions endpoint) =>
        string.IsNullOrWhiteSpace(endpoint.BaseUrl) ? "not-configured"
        : !AiOptions.IsLocalAddress(endpoint.BaseUrl) ? "not-local"
        : null;

    /// <summary>
    /// AI istemcileri yönlendirme izlemez (review I-1): yerel servisin döndürdüğü bir yönlendirme öğrenci verisini (döküm, ses)
    /// yerel olmayan bir adrese taşıyabilirdi; 3xx "kullanılamıyor" olur.
    /// </summary>
    private static SocketsHttpHandler NoRedirects() => new() { AllowAutoRedirect = false };

    private static void Point(HttpClient client, AiEndpointOptions endpoint)
    {
        client.BaseAddress = new Uri(endpoint.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(endpoint.TimeoutSeconds);
    }
}
