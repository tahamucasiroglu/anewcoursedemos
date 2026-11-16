using System.Globalization;
using System.Threading.RateLimiting;
using Konsept.Modules.Identity.Application;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Konsept.Modules.Identity.Infrastructure.RateLimiting;

/// <summary>
/// Giriş/kayıt hız sınırı (spec 0002 Req 9, plan Risk 5): yalnızca Api'de, istemci IP'si başına
/// (<see cref="ClientAddress"/>) <see cref="FixedBlockRateLimiter"/>. Reddedilen istek servise ulaşmaz (hak sayacı
/// değişmez) ve 429 ProblemDetails <c>auth.too_many_attempts</c> döner.
/// </summary>
public static class AuthRateLimiting
{
    /// <summary>Hız sınırını kaydeder (<see cref="AuthPolicies.AuthRateLimit"/> politikası).</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthPolicies.AuthRateLimit, context =>
            {
                var time = context.RequestServices.GetRequiredService<TimeProvider>();
                return RateLimitPartition.Get(ClientAddress.Resolve(context), _ => new FixedBlockRateLimiter(time));
            });

            // Ajan eşleştirme (spec 0012 Req 8): aynı sınır, ayrı sayaç (giriş denemeleri eşleştirmeyi tüketmesin).
            options.AddPolicy(AuthPolicies.AgentPairingRateLimit, context =>
            {
                var time = context.RequestServices.GetRequiredService<TimeProvider>();
                return RateLimitPartition.Get(ClientAddress.Resolve(context), _ => new FixedBlockRateLimiter(time));
            });
            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                var error = IdentityErrors.TooManyAttempts;
                var problem = new ProblemDetails { Status = StatusCodes.Status429TooManyRequests, Title = error.Message };
                problem.Extensions["code"] = error.Code;
                problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
                await response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
            };
        });
        return services;
    }
}
