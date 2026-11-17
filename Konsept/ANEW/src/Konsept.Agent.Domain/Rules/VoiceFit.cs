using System.Globalization;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Sahne sesini sahne süresine oturtma (spec 0028 Req 8, 9): konuşma sahneden uzunsa en çok <see cref="MaxTempo"/> kat hızlandırılır, kısaysa
/// sessizlikle tamamlanır, sahneden uzun kalan kısım kesilir. Çıktı birleştirmenin beklediği biçimdedir (48 kHz stereo PCM); konuşmanın oturtulmuş
/// süresi altyazı zamanlaması için kaydedilir.
/// </summary>
public static class VoiceFit
{
    /// <summary>En çok hızlandırma.</summary>
    public const double MaxTempo = 1.6;

    /// <summary>Konuşma süresinin kaydedildiği dosya (sahne klasöründe; değişmez kültürde saniye).</summary>
    public const string SpeechFile = "voice.speech";

    /// <summary>Ham (oturtulmamış) konuşma dosyası.</summary>
    public const string RawFile = "voice.raw.wav";

    /// <summary>Oturtulmuş ses dosyası (birleştirme bunu okur).</summary>
    public const string VoiceFile = "voice.wav";

    /// <summary>Sahne sesinin işaret dosyası (aynı metin/model/dil/süreyle devamda yeniden üretilmez).</summary>
    public const string MarkerFile = "voice.marker";

    /// <summary>Hızlandırma ve oturtulmuş konuşma süresi.</summary>
    /// <param name="speechSeconds">Ham konuşma süresi.</param>
    /// <param name="sceneSeconds">Sahne süresi.</param>
    /// <returns>Hız katsayısı (≥ 1) ve sahnedeki konuşma süresi (≤ sahne).</returns>
    public static (double Tempo, double SpeechSeconds) Plan(double speechSeconds, double sceneSeconds)
    {
        if (speechSeconds <= 0 || sceneSeconds <= 0)
        {
            return (1, 0);
        }

        var tempo = speechSeconds > sceneSeconds ? Math.Min(MaxTempo, speechSeconds / sceneSeconds) : 1;
        return (tempo, Math.Min(sceneSeconds, speechSeconds / tempo));
    }

    /// <summary>FFmpeg argümanları: (gerekirse) hızlandır, sessizlikle doldur, sahne süresinde kes; 48 kHz stereo PCM.</summary>
    /// <param name="raw">Ham ses.</param>
    /// <param name="output">Çıktı.</param>
    /// <param name="tempo">Hız katsayısı.</param>
    /// <param name="sceneSeconds">Sahne süresi.</param>
    /// <returns>Argümanlar.</returns>
    public static IReadOnlyList<string> Arguments(string raw, string output, double tempo, double sceneSeconds)
    {
        var c = CultureInfo.InvariantCulture;
        var filter = tempo > 1.0005 ? "atempo=" + tempo.ToString("0.####", c) + ",apad" : "apad";
        return ["-y", "-hide_banner", "-loglevel", "error", "-i", raw, "-af", filter, "-t", sceneSeconds.ToString("0.###", c), "-ar", "48000", "-ac", "2", "-c:a", "pcm_s16le", output];
    }

    /// <summary>FFmpeg argümanları: metni olmayan sahne için sahne süresinde sessiz ses (48 kHz stereo PCM).</summary>
    /// <param name="output">Çıktı.</param>
    /// <param name="sceneSeconds">Sahne süresi.</param>
    /// <returns>Argümanlar.</returns>
    public static IReadOnlyList<string> SilenceArguments(string output, double sceneSeconds) =>
        ["-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "anullsrc=r=48000:cl=stereo", "-t", sceneSeconds.ToString("0.###", CultureInfo.InvariantCulture), "-c:a", "pcm_s16le", output];
}
