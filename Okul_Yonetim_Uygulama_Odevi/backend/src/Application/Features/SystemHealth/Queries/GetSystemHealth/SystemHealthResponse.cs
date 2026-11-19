using System.Text.Json.Serialization;

namespace OkulYonetim.Application.Features.SystemHealth.Queries.GetSystemHealth;

/// <summary>
/// Sistem durumu yanıtı. Yalnızca durum ("healthy" / "unavailable") ve Türkçe mesaj taşır;
/// sürüm, sunucu, bağlantı veya hata ayrıntısı bilinçli olarak yoktur (spec 0001). Durum metni tek yerde,
/// <c>From</c> içinde <c>SystemHealthStatus</c>'tan türetilir; <c>Kind</c> yalnızca sunucu içindir (JSON'a yazılmaz).
/// </summary>
public sealed record SystemHealthResponse(string Status, string Message)
{
    [JsonIgnore]
    public SystemHealthStatus Kind { get; init; }

    public static SystemHealthResponse From(SystemHealthStatus kind, string message) =>
        new(kind == SystemHealthStatus.Healthy ? "healthy" : "unavailable", message) { Kind = kind };
}
