using System.Net.Mime;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.WebAPI.Auth;

/// <summary>
/// Giriş ucu için IP başına dakikada <c>Auth:LoginRateLimitPerMinute</c> (varsayılan 20) istek sınırı (spec 0004, Ö-4).
/// Aşılınca 429 ve Türkçe "Çok fazla istek" mesajı (ProblemDetails). E-posta başına kilitten bağımsızdır.
/// </summary>
public static class LoginRateLimit
{
    public const string PolicyName = "login";
    private const int DefaultPerMinute = 20;
    private static readonly string[] Locales = ["tr"];

    public static IServiceCollection AddLoginRateLimit(this IServiceCollection services, IConfiguration configuration)
    {
        int perMinute = configuration.GetValue("Auth:LoginRateLimitPerMinute", DefaultPerMinute);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;
            options.AddPolicy(PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        HttpContext http = context.HttpContext;
        ILocalizationService localization = http.RequestServices.GetRequiredService<ILocalizationService>();
        string message = await localization.GetLocalizedAsync("TooManyRequests", Locales, "Identity").ConfigureAwait(false);
        ProblemDetails problem = new() { Status = StatusCodes.Status429TooManyRequests, Title = message, Detail = message };
        http.RequestServices.GetRequiredService<ILogger<OnRejectedContext>>()
            .LogWarning("Giriş hız sınırı aşıldı: {Path}", http.Request.Path);
        await http.Response.WriteAsJsonAsync(problem, options: null, contentType: MediaTypeNames.Application.ProblemJson, cancellationToken).ConfigureAwait(false);
    }
}
