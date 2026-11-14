namespace TahaKonsept.Shared.Contracts.Common;

/// <summary>
/// <c>GET /api/health</c> yanıtı: durum, uygulama sürümü (VERSION dosyası), ortam, sunucu zamanı ve veritabanı durumu.
/// </summary>
public sealed record HealthResponse
{
    /// <summary>Durum metni: <c>ok</c> (her şey hazır) ya da <c>degraded</c> (API çalışıyor, veritabanı henüz hazır değil).</summary>
    public string Status { get; init; } = "ok";

    /// <summary>Uygulama sürümü (ör. <c>0.1.0</c>).</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>Çalışma ortamı (Development/Production).</summary>
    public string Environment { get; init; } = string.Empty;

    /// <summary>Sunucu zamanı (UTC).</summary>
    public DateTimeOffset ServerTimeUtc { get; init; }

    /// <summary>Veritabanı durumu: <c>ready</c> ya da <c>unavailable</c>.</summary>
    public string Database { get; init; } = "ready";

    /// <summary>Veritabanı hazır değilse son hata mesajı.</summary>
    public string? DatabaseError { get; init; }
}
