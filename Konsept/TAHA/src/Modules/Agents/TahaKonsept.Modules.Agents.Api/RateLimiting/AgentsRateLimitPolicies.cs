using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace TahaKonsept.Modules.Agents.Api.RateLimiting;

/// <summary>
/// Anonim cihaz eşleme uçlarının istek sınırları (IP başına sabit pencere): <see cref="PairingStart"/> (<c>device/start</c>, her çağrı DB satırı
/// yazar) dakikada <see cref="StartPermitsPerMinute"/>, <see cref="PairingToken"/> (<c>device/token</c>, 5 sn'de bir yoklama) dakikada
/// <see cref="TokenPermitsPerMinute"/>. Aşılınca 429. <see cref="Register"/> politikaları ekler; host <c>UseRateLimiter</c> çağırır.
/// </summary>
public static class AgentsRateLimitPolicies
{
    /// <summary>Eşleme başlatma politikası.</summary>
    public const string PairingStart = "agents-pairing-start";

    /// <summary>Token yoklama politikası.</summary>
    public const string PairingToken = "agents-pairing-token";

    /// <summary>IP başına dakikada eşleme başlatma.</summary>
    public const int StartPermitsPerMinute = 10;

    /// <summary>IP başına dakikada token yoklaması.</summary>
    public const int TokenPermitsPerMinute = 60;

    /// <summary>Politikaları ekler.</summary>
    /// <param name="options">Sınırlayıcı ayarları.</param>
    public static void Register(RateLimiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(PairingStart, context => PerIp(context, StartPermitsPerMinute));
        options.AddPolicy(PairingToken, context => PerIp(context, TokenPermitsPerMinute));
    }

    private static RateLimitPartition<string> PerIp(HttpContext context, int permits) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}
