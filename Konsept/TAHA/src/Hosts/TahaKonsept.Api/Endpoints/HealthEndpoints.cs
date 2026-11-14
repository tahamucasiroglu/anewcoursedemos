using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Common;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Api.Endpoints;

/// <summary>
/// Sağlık ucu: <c>GET /api/health</c> (anonim) → <see cref="HealthResponse"/> (durum, sürüm, ortam, sunucu zamanı, veritabanı durumu). Veritabanı hazır değilse de 200 döner, <c>status=degraded</c>.
/// </summary>
internal static class HealthEndpoints
{
    /// <summary>Uç yolu.</summary>
    public const string Path = "/api/health";

    /// <summary>Sağlık ucunu eşler.</summary>
    /// <param name="endpoints">Uç oluşturucu.</param>
    /// <returns>Aynı uç oluşturucu.</returns>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(Path, (IHostEnvironment environment, IClock clock, IDatabaseStatus database) => TypedResults.Ok(new HealthResponse
            {
                Status = database.IsReady ? "ok" : "degraded",
                Database = database.IsReady ? "ready" : "unavailable",
                DatabaseError = database.IsReady ? null : database.LastError,
                Version = AppVersion.Of(typeof(HealthEndpoints).Assembly),
                Environment = environment.EnvironmentName,
                ServerTimeUtc = clock.UtcNow,
            }))
            .AllowAnonymous()
            .WithName("Health");
        return endpoints;
    }
}
