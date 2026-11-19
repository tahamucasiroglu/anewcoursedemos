namespace OkulYonetim.Application.Features.SystemHealth.Queries.GetSystemHealth;

/// <summary>
/// Sistem durumu yanıtı. Yalnızca durum ("healthy" / "unavailable") ve Türkçe mesaj taşır;
/// sürüm, sunucu, bağlantı veya hata ayrıntısı bilinçli olarak yoktur (spec 0001).
/// </summary>
public sealed record SystemHealthResponse(string Status, string Message);
