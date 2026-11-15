using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TahaKonsept.Web.Infrastructure.RateLimiting;

/// <summary>
/// Giriş ve kayıt Ajax uçlarının istek sınırları (tarayıcı IP'si başına sabit 1 dk pencere): <see cref="Login"/> dakikada
/// <c>RateLimits:LoginPerMinute</c> (varsayılan 20), <see cref="Register"/> dakikada <c>RateLimits:RegisterPerMinute</c> (varsayılan 10).
/// Aşılınca 429 + Türkçe ProblemDetails (form uyarı kutusunda görünür). <see cref="Configure"/> politikaları ekler.
/// </summary>
public static class AccountRateLimitPolicies
{
    /// <summary>Giriş politikası.</summary>
    public const string Login = "account-login";

    /// <summary>Kayıt politikası.</summary>
    public const string Register = "account-register";

    /// <summary>Politikaları ve ret cevabını ekler.</summary>
    /// <param name="options">Sınırlayıcı ayarları.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public static void Configure(RateLimiterOptions options, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        var login = Permits(configuration, "RateLimits:LoginPerMinute", 20);
        var register = Permits(configuration, "RateLimits:RegisterPerMinute", 10);
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(Login, context => PerIp(context, login));
        options.AddPolicy(Register, context => PerIp(context, register));
        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";
            await context.HttpContext.Response.WriteAsJsonAsync(
                new { status = 429, title = "Çok fazla deneme yapıldı. Lütfen bir dakika sonra tekrar deneyin." },
                ct);
        };
    }

    private static int Permits(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : fallback;

    private static RateLimitPartition<string> PerIp(HttpContext context, int permits) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}
