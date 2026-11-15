using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace TahaKonsept.Modules.Identity.Api.RateLimiting;

/// <summary>
/// Anonim kayıt ucunun istek sınırı (IP başına sabit pencere, dakikada <see cref="DefaultRegisterPermitsPerMinute"/>; yapılandırma
/// <see cref="RegisterPermitsKey"/> ile değişir). Aşılınca 429. Giriş ayrıca hesap kilidiyle (10 hatalı deneme → 5 dk) korunur; Web
/// tarafında da tarayıcı IP'si başına sınır vardır. <see cref="Register"/> politikayı ekler; host <c>UseRateLimiter</c> çağırır.
/// </summary>
public static class IdentityRateLimitPolicies
{
    /// <summary>Kayıt politikası adı.</summary>
    public const string RegisterPolicy = "identity-register";

    /// <summary>Dakikadaki kayıt isteği sınırı için yapılandırma anahtarı.</summary>
    public const string RegisterPermitsKey = "Identity:RateLimits:RegisterPerMinute";

    /// <summary>Varsayılan dakikadaki kayıt isteği.</summary>
    public const int DefaultRegisterPermitsPerMinute = 10;

    /// <summary>Politikayı ekler.</summary>
    /// <param name="options">Sınırlayıcı ayarları.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public static void Register(RateLimiterOptions options, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        var permits = int.TryParse(configuration[RegisterPermitsKey], NumberStyles.Integer, CultureInfo.InvariantCulture, out var configured) && configured > 0
            ? configured
            : DefaultRegisterPermitsPerMinute;
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(RegisterPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    }
}
