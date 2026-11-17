using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Konsept.Modules.Identity.Application;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Base;
using Konsept.Modules.Identity.Application.Concrete;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Application.Validators;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Identity.Infrastructure.Authentication;
using Konsept.Modules.Identity.Infrastructure.Hosting;
using Konsept.Modules.Identity.Infrastructure.Pairing.Concrete;
using Konsept.Modules.Identity.Infrastructure.Persistence;
using Konsept.Modules.Identity.Infrastructure.Persistence.Concrete;
using Konsept.Modules.Identity.Infrastructure.RateLimiting;
using Konsept.Modules.Identity.Infrastructure.Security;
using Konsept.Modules.Identity.Infrastructure.Security.Concrete;
using Konsept.Shared.Infrastructure.Http;
using Konsept.Shared.Infrastructure.Http.Concrete;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Konsept.Modules.Identity.Infrastructure;

/// <summary>
/// Identity modülünün servis kaydı (kalıcılık, servisler, JWT + servis anahtarı kimlik şemaları, giriş/kayıt hız sınırı,
/// ilk admin ayarı ve açılış rol senkronu) ve açılış migration'ı.
/// </summary>
public static class IdentityModule
{
    private static readonly Action<ILogger, Exception?> EphemeralKeyWarning = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(2101, "EphemeralSigningKey"),
        "Auth:SigningKey yapılandırılmadı; Development için süreç başına rastgele imza anahtarı üretildi (yeniden başlatmada oturumlar düşer).");

    /// <summary>Modülün servislerini kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="connectionString">PostgreSQL bağlantı metni.</param>
    /// <param name="configuration">Yapılandırma (<c>Auth:SigningKey</c>, <c>Identity:AdminEmail</c>).</param>
    /// <param name="isDevelopment">Development ortamı mı (imza anahtarı yoksa rastgele üretilir; değilse açılış hatası).</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, string connectionString, IConfiguration configuration, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddDbContext<IdentityModuleDbContext>(o => o.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityModuleDbContext.SchemaName)));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddSingleton<IPasswordHashing, AspNetPasswordHashing>();
        services.AddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();
        var jwt = CreateJwtSettings(configuration, isDevelopment);
        services.AddSingleton(jwt);
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<AuthServiceDependencies>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ISessionRevoker>(sp => sp.GetRequiredService<ISessionService>());
        services.AddScoped<IUserAccountReader, UserAccountReader>(); // Günlük geçmişinin ilk günü (spec 0021 Req 6).
        services.AddScoped<IUserDirectory, UserDirectory>(); // Admin istatistikleri ve aktiviteleri (spec 0010).
        services.AddScoped<IUserBanRepository, UserBanRepository>(); // Admin kullanıcılar ve ban (spec 0024).
        services.AddScoped<AdminUserDependencies>();
        services.AddScoped<IAdminUserService, AdminUserService>();

        // Ajan cihaz eşleştirme (spec 0012): kodlar bellekte (tek süreç), cihazlar identity.agent_devices'ta.
        services.AddSingleton<IPairingStore, InMemoryPairingStore>();
        services.AddScoped<IAgentDeviceRepository, AgentDeviceRepository>();
        services.AddScoped<IAgentDeviceRevoker, AgentDeviceRevoker>();
        services.AddScoped<IAgentTokenValidator, AgentTokenValidator>();
        services.AddScoped<AgentPairingDependencies>();
        services.AddScoped<IAgentPairingService, AgentPairingService>();

        // İlk admin (spec 0011 Req 5): ayar her açılışta okunur; açılış senkronu arka planda rolleri ayara eşitler.
        // Model yetkileri (spec 0013 Req 2): ayar yoksa her rol tüm modelleri kullanır; kısıtlama yalnızca ayarla (ekran yok).
        services.AddSingleton(new IdentityOptions
        {
            AdminEmail = configuration[IdentityOptions.AdminEmailConfigurationKey],
            UserModels = IdentityOptions.ParseModelList(configuration["Identity:ModelPermissions:User"]),
            AdminModels = IdentityOptions.ParseModelList(configuration["Identity:ModelPermissions:Admin"]),
        });
        services.AddScoped<IAdminRoleSync, AdminRoleSync>();
        services.AddScoped<IAdminOverviewService, AdminOverviewService>();
        services.AddHostedService<AdminRoleSyncHostedService>();
        services.AddSingleton<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddScoped<SessionValidatingJwtEvents>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.EventsType = typeof(SessionValidatingJwtEvents);
            })
            .AddScheme<AuthenticationSchemeOptions, ServiceKeyAuthenticationHandler>(AuthPolicies.ServiceKeyScheme, null)
            .AddScheme<AuthenticationSchemeOptions, AgentTokenAuthenticationHandler>(AuthPolicies.AgentScheme, null);
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<TimeProvider>((o, time) => o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = JwtSettings.Issuer,
                ValidAudience = JwtSettings.Audience,
                IssuerSigningKey = jwt.Key,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "name",

                // Süre aynı saat kaynağından doğrulanır (plan Risk 6): testte ilerletilen saat gerçek saatle çelişmez.
                LifetimeValidator = (notBefore, expires, _, _) =>
                {
                    var now = time.GetUtcNow().UtcDateTime;
                    return expires is { } e && e > now && (notBefore is null || notBefore.Value <= now);
                },
            });
        services.AddAuthRateLimiting();
        return services;
    }

    /// <summary>İmza anahtarı rastgele üretildiyse (Development) bir kez uyarı yazar; anahtarın kendisi yazılmaz.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    public static void WarnIfEphemeralSigningKey(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.GetRequiredService<JwtSettings>().KeyGenerated)
        {
            EphemeralKeyWarning(services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(IdentityModule).FullName!), null);
        }
    }

    /// <summary>Modülün şemasını bekleyen migration'larla günceller (boş veritabanında oluşturur).</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public static async Task MigrateIdentityDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    private static JwtSettings CreateJwtSettings(IConfiguration configuration, bool isDevelopment)
    {
        var configured = configuration[JwtSettings.SigningKeyConfigurationKey];
        if (!string.IsNullOrEmpty(configured))
        {
            var bytes = Encoding.UTF8.GetBytes(configured);
            return bytes.Length < JwtSettings.MinKeyBytes
                ? throw new InvalidOperationException($"{JwtSettings.SigningKeyConfigurationKey} en az {JwtSettings.MinKeyBytes} bayt olmalı.")
                : new JwtSettings(new SymmetricSecurityKey(bytes), KeyGenerated: false);
        }

        return isDevelopment
            ? new JwtSettings(new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(48)), KeyGenerated: true)
            : throw new InvalidOperationException($"{JwtSettings.SigningKeyConfigurationKey} ayarı eksik (Development dışında zorunlu).");
    }
}
