namespace OkulYonetim.Infrastructure.Ai;

/// <summary>
/// Tek bir yerel AI servisinin ayarı (spec 0012): adres (boşsa özellik "kullanılamıyor"), model adı (yalnızca metin servisi —
/// ör. <c>gemma3:4b</c>) ve zaman aşımı (saniye; ilk model yüklemesi dahil). Yeniden deneme yoktur (plan 0012 Risks 4).
/// </summary>
public sealed class AiEndpointOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 60;
}
