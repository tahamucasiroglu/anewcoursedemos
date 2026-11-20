namespace OkulYonetim.Application.Ports;

/// <summary>
/// Metin üreten yapay zekâ portu (spec 0012 C-1): sabit talimat + içerik → metin. MVP'de bu bilgisayardaki Ollama
/// (<c>Infrastructure/Ai/OllamaTextGenerator</c>); adres ayarlanmamışsa her çağrı "kullanılamıyor". Servis kapalı, zaman
/// aşımı, hata yanıtı ya da boş yanıt <see cref="AiUnavailableException"/> fırlatır. İçeriğe yalnızca görev için gereken
/// metin konur (veri minimizasyonu — <c>docs/security.md</c>).
/// </summary>
public interface IAiTextGenerator
{
    Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken);
}
