using System.Globalization;
using System.Text;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Birleştirmenin sabit şablonu (spec 0017 Req 2–4, 14a; Karar 5): altyazı LLM'den değil buradan gelir — ASS dosyasında her kelime, sahnenin
/// süresi içinde karakter ağırlığıyla zamanlanır ve okunduğu aralıkta vurgu rengiyle gösterilir (cümle satırı 7 kelimelik parçalar).
/// İlerleme çubuğu videonun üst kenarında <c>overlay</c> ile <c>x = -w + w·t/T</c> (ilk karede boş, sonda dolu, yarıda yarı). Ses yoksa
/// sessiz iz (<c>anullsrc</c>) ve altyazı yok. Kodlama ekran kartında (<see cref="Encoder"/>); kodlayıcı açılamazsa CPU'ya düşülmez.
/// </summary>
public static class MergeTemplate
{
    /// <summary>Video kodlayıcı (NVIDIA NVENC; Req 14a).</summary>
    public const string Encoder = "h264_nvenc";

    /// <summary>NVENC yoksa yazılım kodlayıcı (B-117; pinli LGPL FFmpeg'de libx264 yok).</summary>
    public const string SoftwareEncoder = "libopenh264";

    /// <summary>Yazılım kodlayıcının hedef bit hızı.</summary>
    public const string SoftwareBitrate = "8M";

    /// <summary>NVENC yoklaması: küçük bir deneme kodlaması (çıkış 0 → kullanılabilir).</summary>
    public static readonly IReadOnlyList<string> NvencProbeArguments =
        ["-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "color=c=black:s=256x256:d=0.2", "-frames:v", "3", "-c:v", Encoder, "-f", "null", "-"];

    /// <summary>İlerleme çubuğunun yüksekliği (piksel).</summary>
    public const int BarHeight = 12;

    /// <summary>İlerleme çubuğunun rengi.</summary>
    public const string BarColor = "0xF5B700";

    /// <summary>Okunan kelimenin rengi (ASS, BGR).</summary>
    public const string HighlightColor = "&H0000B7F5&";

    /// <summary>Altyazı metninin rengi (ASS, BGR).</summary>
    public const string TextColor = "&H00FFFFFF&";

    /// <summary>Bir altyazı satırındaki en çok kelime.</summary>
    public const int WordsPerLine = 7;

    /// <summary>Altyazı dosyasının adı (iş klasöründe).</summary>
    public const string SubtitleFile = "subs.ass";

    /// <summary>Biçimin çözünürlüğü.</summary>
    /// <param name="format"><c>portrait</c> ya da <c>landscape</c>.</param>
    /// <returns>Genişlik, yükseklik.</returns>
    public static (int Width, int Height) Size(string format) =>
        string.Equals(format, "portrait", StringComparison.Ordinal) ? (1080, 1920) : (1920, 1080);

    /// <summary>ASS altyazı dosyası: sahne sırasıyla, her kelime kendi aralığında vurgulu.</summary>
    /// <param name="scenes">Sahneler (süre sn, seslendirme metni).</param>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <returns>Dosya içeriği.</returns>
    public static string Subtitles(IReadOnlyList<(double Seconds, string Narration)> scenes, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        var c = CultureInfo.InvariantCulture;
        var text = new StringBuilder();
        text.Append("[Script Info]\nScriptType: v4.00+\nPlayResX: ").Append(width.ToString(c)).Append("\nPlayResY: ").Append(height.ToString(c)).Append("\n\n");
        text.Append("[V4+ Styles]\nFormat: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, ")
            .Append("StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding\n");
        text.Append("Style: Default,Arial,").Append((height / 18).ToString(c)).Append(",&H00FFFFFF,&H00FFFFFF,&H00000000,&H80000000,1,0,0,0,100,100,0,0,1,3,0,2,60,60,")
            .Append((height / 10).ToString(c)).Append(",1\n\n");
        text.Append("[Events]\nFormat: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text\n");
        var offset = 0d;
        foreach (var (seconds, narration) in scenes)
        {
            var words = Clean(narration).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var total = words.Sum(w => w.Length);
            var at = offset;
            for (var first = 0; first < words.Length; first += WordsPerLine)
            {
                var line = words.Skip(first).Take(WordsPerLine).ToArray();
                for (var i = 0; i < line.Length; i++)
                {
                    var length = seconds * line[i].Length / total;
                    text.Append("Dialogue: 0,").Append(Time(at)).Append(',').Append(Time(at + length)).Append(",Default,,0,0,0,,")
                        .Append(string.Join(' ', line.Select((w, j) => j == i ? "{\\c" + HighlightColor + "}" + w + "{\\c" + TextColor + "}" : w)))
                        .Append('\n');
                    at += length;
                }
            }

            offset += seconds;
        }

        return text.ToString();
    }

    /// <summary>FFmpeg filtre grafiği: altyazı (açıksa) ve üst kenarda ilerleme çubuğu (açıksa); çıkış etiketi <c>[v]</c>.</summary>
    /// <param name="subtitles">Altyazı.</param>
    /// <param name="progressBar">İlerleme çubuğu.</param>
    /// <param name="totalSeconds">Video süresi.</param>
    /// <param name="width">Genişlik.</param>
    /// <returns>Filtre.</returns>
    public static string Filter(bool subtitles, bool progressBar, double totalSeconds, int width)
    {
        var c = CultureInfo.InvariantCulture;
        var parts = new List<string>();
        var label = "0:v";
        if (subtitles)
        {
            parts.Add("[" + label + "]subtitles=" + SubtitleFile + "[sub]");
            label = "sub";
        }

        if (progressBar)
        {
            parts.Add("color=c=" + BarColor + ":s=" + width.ToString(c) + "x" + BarHeight.ToString(c) + "[bar]");
            parts.Add("[" + label + "][bar]overlay=x='-w+w*t/" + Math.Max(0.001, totalSeconds).ToString("0.###", c) + "':y=0:shortest=1[barred]");
            label = "barred";
        }

        parts.Add("[" + label + "]format=yuv420p[v]");
        return string.Join(';', parts);
    }

    /// <summary>FFmpeg argümanları: sahne klipleri (concat), ses (concat ya da sessiz iz), filtre, NVENC, AAC.</summary>
    /// <param name="clipList">Klip listesi dosyası.</param>
    /// <param name="audioList">Ses listesi dosyası; sessizse <c>null</c>.</param>
    /// <param name="filter">Filtre grafiği.</param>
    /// <param name="totalSeconds">Süre.</param>
    /// <param name="output">Çıkış dosyası.</param>
    /// <param name="encoder">Kodlayıcı (<see cref="Encoder"/> ya da NVENC yoksa <see cref="SoftwareEncoder"/>).</param>
    /// <returns>Argümanlar.</returns>
    public static IReadOnlyList<string> Arguments(string clipList, string? audioList, string filter, double totalSeconds, string output, string encoder = Encoder)
    {
        var args = new List<string> { "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", clipList };
        args.AddRange(audioList is null
            ? new[] { "-f", "lavfi", "-t", totalSeconds.ToString("0.###", CultureInfo.InvariantCulture), "-i", "anullsrc=r=48000:cl=stereo" }
            : new[] { "-f", "concat", "-safe", "0", "-i", audioList });
        args.AddRange(new[] { "-filter_complex", filter, "-map", "[v]", "-map", "1:a", "-c:v", encoder });
        if (encoder == SoftwareEncoder)
        {
            args.AddRange(new[] { "-b:v", SoftwareBitrate });
        }

        args.AddRange(new[] { "-c:a", "aac", "-shortest", "-movflags", "+faststart", output });
        return args;
    }

    /// <summary>FFmpeg concat listesi.</summary>
    /// <param name="files">Dosyalar (tam yol).</param>
    /// <returns>İçerik.</returns>
    public static string ConcatList(IEnumerable<string> files) =>
        string.Concat(files.Select(f => "file '" + f.Replace("\\", "/", StringComparison.Ordinal).Replace("'", "'\\''", StringComparison.Ordinal) + "'\n"));

    /// <summary>FFmpeg hatası ekran kartı/kodlayıcı kaynaklı mı (Req 14a: CPU'ya düşülmez, hata olur).</summary>
    /// <param name="error">FFmpeg hata çıktısı.</param>
    /// <returns>GPU kaynaklıysa <c>true</c>.</returns>
    public static bool IsGpuFailure(string? error) =>
        error is not null && (error.Contains("nvenc", StringComparison.OrdinalIgnoreCase) || error.Contains("cuda", StringComparison.OrdinalIgnoreCase)
            || error.Contains("No capable devices", StringComparison.OrdinalIgnoreCase));

    private static string Time(double seconds)
    {
        var cs = (long)Math.Round(seconds * 100);
        var c = CultureInfo.InvariantCulture;
        return (cs / 360000).ToString(c) + ":" + (cs / 6000 % 60).ToString("00", c) + ":" + (cs / 100 % 60).ToString("00", c) + "." + (cs % 100).ToString("00", c);
    }

    private static string Clean(string narration) =>
        narration.Replace('{', '(').Replace('}', ')').Replace('\\', '/').Replace('\n', ' ').Replace('\r', ' ');
}
