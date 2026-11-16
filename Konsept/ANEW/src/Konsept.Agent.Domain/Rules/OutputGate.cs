using System.Globalization;
using System.Text.Json;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Render çıktı kapısı (spec 0018 Req 4, L7): <c>ffprobe</c>'un JSON çıktısına göre klip oynatılabilir bir video mu, boyutu konseptin biçimi
/// mi, süresi beklenene yakın mı. Çıktı hiçbir zaman çalıştırılmaz; yalnızca ffprobe ile okunur.
/// </summary>
public static class OutputGate
{
    /// <summary>ffprobe argümanları (dosya en sona eklenir).</summary>
    public static readonly IReadOnlyList<string> ProbeArguments =
        ["-v", "error", "-show_entries", "stream=codec_type,width,height:format=duration", "-of", "json"];

    /// <summary>Süre sapması toleransı: beklenenin oranı.</summary>
    public const double DurationTolerance = 0.10;

    /// <summary>Süre sapması toleransının alt sınırı (saniye).</summary>
    public const double MinDurationToleranceSeconds = 0.5;

    /// <summary>Çıktının sorununu döndürür; geçerliyse <c>null</c>.</summary>
    /// <param name="probeJson">ffprobe JSON çıktısı (ffprobe başarısızsa <c>null</c>).</param>
    /// <param name="width">Beklenen genişlik.</param>
    /// <param name="height">Beklenen yükseklik.</param>
    /// <param name="seconds">Beklenen süre.</param>
    /// <returns>Sorun ya da <c>null</c>.</returns>
    public static string? Problem(string? probeJson, int width, int height, double seconds)
    {
        if (string.IsNullOrWhiteSpace(probeJson))
        {
            return "not a playable video";
        }

        try
        {
            using var doc = JsonDocument.Parse(probeJson);
            var video = doc.RootElement.TryGetProperty("streams", out var streams) && streams.ValueKind == JsonValueKind.Array
                ? streams.EnumerateArray().FirstOrDefault(s => s.TryGetProperty("codec_type", out var t) && t.GetString() == "video")
                : default;
            if (video.ValueKind != JsonValueKind.Object)
            {
                return "no video stream";
            }

            if (!video.TryGetProperty("width", out var w) || !video.TryGetProperty("height", out var h) || w.GetInt32() != width || h.GetInt32() != height)
            {
                return "unexpected size";
            }

            if (!doc.RootElement.TryGetProperty("format", out var format) || !format.TryGetProperty("duration", out var d)
                || !double.TryParse(d.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var duration))
            {
                return "unknown duration";
            }

            return Math.Abs(duration - seconds) > Math.Max(MinDurationToleranceSeconds, seconds * DurationTolerance) ? "unexpected duration" : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            return "not a playable video";
        }
    }
}
