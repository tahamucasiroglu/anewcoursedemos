using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.JsonWebTokens;
using OkulYonetim.Application.Features.Identity.Sessions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Security;

namespace OkulYonetim.WebAPI.Auth;

/// <summary>
/// Kimlik doğrulama ve yetki (spec 0004): JWT imzası/süresi doğrulanır, ardından her istekte oturum veritabanından
/// kontrol edilir (<see cref="SessionAuthenticator"/>: iptal, 7 gün, kurum eşleşmesi) ve kurum/şube filtresi doldurulur.
/// <b>Varsayılan: reddet</b> — açıkça <c>[AllowAnonymous]</c> olmayan her uç giriş ister (giriş, yenileme, sistem durumu
/// dışında). Kuruma bağlı uçlar ayrıca okul seçilmiş olmasını ister. İmza anahtarı eksikse API başlamaz.
/// IP adresi nginx'in X-Forwarded-For başlığından alınır; yalnızca yerel vekil (127.0.0.1/::1) güvenilir.
/// </summary>
public static class AuthSetup
{
    public const string TenantSelectedPolicy = "TenantSelected";

    /// <summary>Seçili okulda kurucu/yönetici (spec 0005); kurucu bilgisi her istekte veritabanından okunur (rol alınınca bir sonraki istekte geçerli).</summary>
    public const string TenantFounderPolicy = "TenantFounder";

    public static IServiceCollection AddOkulAuth(this IServiceCollection services, IConfiguration configuration)
    {
        JwtOptions options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrEmpty(options.SigningKey) && IsDesignTime())
        {
            // Derleme anındaki OpenAPI üretimi ve EF migration araçları uygulamayı istek almadan kurar; geçici anahtar.
            options.SigningKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            services.PostConfigure<JwtOptions>(o => o.SigningKey = options.SigningKey);
        }

        Microsoft.IdentityModel.Tokens.TokenValidationParameters validation = options.CreateValidationParameters();

        services.Configure<ForwardedHeadersOptions>(forwarded =>
            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
        services.AddScoped<IClientInfo, HttpClientInfo>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwt =>
        {
            jwt.MapInboundClaims = false;
            jwt.TokenValidationParameters = validation;
            jwt.Events = new JwtBearerEvents { OnTokenValidated = ValidateSessionAsync };
        });

        // Anahtarın süresi uygulamanın saatiyle (TimeProvider) doğrulanır: anahtarı üreten saatle aynı (testte sahte saat).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<TimeProvider>((jwt, time) =>
        {
            TimeSpan skew = jwt.TokenValidationParameters.ClockSkew;
            jwt.TokenValidationParameters.LifetimeValidator = (notBefore, expires, _, _) =>
            {
                DateTime now = time.GetUtcNow().UtcDateTime;
                return expires is { } exp && exp > now - skew && (notBefore is not { } nbf || nbf <= now + skew);
            };
        });
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(TenantSelectedPolicy, policy => policy.RequireAuthenticatedUser().RequireClaim(JwtTokenService.TenantClaim))
            .AddPolicy(TenantFounderPolicy, policy => policy.RequireAuthenticatedUser().RequireClaim(JwtTokenService.TenantClaim)
                .RequireAssertion(context => context.Resource is HttpContext http && http.RequestServices.GetRequiredService<ICurrentSession>().IsFounder));
        return services;
    }

    private static bool IsDesignTime() =>
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name is "GetDocument.Insider" or "ef";

    private static async Task ValidateSessionAsync(TokenValidatedContext context)
    {
        System.Security.Claims.ClaimsPrincipal? principal = context.Principal;
        bool parsed = Guid.TryParse(principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out Guid userId)
            & Guid.TryParse(principal?.FindFirst(JwtTokenService.SessionClaim)?.Value, out Guid sessionId);
        string? tenantClaim = principal?.FindFirst(JwtTokenService.TenantClaim)?.Value;
        Guid? tenantId = Guid.TryParse(tenantClaim, out Guid tenant) ? tenant : null;
        if (!parsed || (tenantClaim is not null && tenantId is null))
        {
            context.Fail("Oturum bilgisi okunamadı");
            return;
        }

        SessionAuthenticator authenticator = context.HttpContext.RequestServices.GetRequiredService<SessionAuthenticator>();
        if (!await authenticator.AuthenticateAsync(userId, sessionId, tenantId, context.HttpContext.RequestAborted).ConfigureAwait(false))
        {
            context.Fail("Oturum geçersiz");
        }
    }
}
