using System.Text.Json.Serialization;

namespace OkulYonetim.Application.Features.SystemHealth.Queries.GetSystemHealth;

/// <summary>
/// Sistem durumu yanıtı. Yalnızca durum ("healthy" / "unavailable") ve Türkçe mesaj taşır;
/// sürüm, sunucu, bağlantı veya hata ayrıntısı bilinçli olarak yoktur (spec 0001). Tek doğruluk kaynağı
/// <c>Kind</c>'dır (JSON'a yazılmaz); <c>Status</c> metni yalnızca kurucuda ondan hesaplanır, bu yüzden ikisi
/// ayrışamaz. Dışarıdan yalnızca <c>From</c> ile oluşturulur.
/// </summary>
public sealed class SystemHealthResponse
{
    private SystemHealthResponse(SystemHealthStatus kind, string message)
    {
        this.Kind = kind;
        this.Status = kind == SystemHealthStatus.Healthy ? "healthy" : "unavailable";
        this.Message = message;
    }

    [JsonInclude]
    [JsonRequired]
    public string Status { get; private init; }

    [JsonInclude]
    [JsonRequired]
    public string Message { get; private init; }

    [JsonIgnore]
    public SystemHealthStatus Kind { get; }

    public static SystemHealthResponse From(SystemHealthStatus kind, string message) => new(kind, message);
}
