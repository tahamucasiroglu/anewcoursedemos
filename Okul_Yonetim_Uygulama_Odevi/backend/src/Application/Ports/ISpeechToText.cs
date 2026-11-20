namespace OkulYonetim.Application.Ports;

/// <summary>
/// Ses → metin portu (spec 0012 C-1): sesi Türkçe yazıya döker ve süresini (saniye) bildirir. MVP'de bu bilgisayarda çalışan
/// yerel ses servisi (<c>tools/ai/stt_server.py</c>, faster-whisper). Ses yalnızca bellekte işlenir, diske yazılmaz. Servis
/// ayarlanmamış/kapalı, zaman aşımı ya da hata yanıtı <see cref="AiUnavailableException"/> fırlatır.
/// </summary>
public interface ISpeechToText
{
    Task<SpeechToTextResult> TranscribeAsync(Stream audio, string contentType, CancellationToken cancellationToken);
}
