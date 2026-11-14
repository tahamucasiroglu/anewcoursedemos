using System.Globalization;
using System.Text;
using TahaKonsept.Agent.Engines.Video.Subtitles.Base;

namespace TahaKonsept.Agent.Engines.Video.Subtitles.Concrete;

/// <summary>
/// ASS (Advanced SubStation Alpha) karaoke altyazı yazıcısı. Her satır bir <c>Dialogue</c>; her kelime <c>{\kNN}</c> etiketiyle
/// (santisaniye) söylendiği anda beyazdan (SecondaryColour) vurgu rengine (PrimaryColour, turuncu-sarı) geçer. Kalın, dış çizgili,
/// alt-orta hizalı ve alt üçte bir bölgede (MarginV dikeyde yüksekliğin %22'si, yatayda %12'si). Dikey/yatay için farklı punto
/// (<see cref="FontSizeFor"/>). PlayRes video boyutuna eşitlenir; Türkçe karakterler UTF-8 yazılır (Encoding 162 = Türkçe).
/// </summary>
public sealed class AssKaraokeSubtitleWriter : CaptionSubtitleWriterBase
{
    /// <summary>Altyazı yazı tipi (Windows'ta her zaman bulunan, Türkçe karakterli).</summary>
    public const string FontName = "Arial";

    /// <summary>Stil adı.</summary>
    public const string StyleName = "Karaoke";

    /// <inheritdoc />
    public override string FileExtension => ".ass";

    /// <summary>Yerleşime göre punto (dikey 1080x1920 → 76, yatay 1920x1080 → 62).</summary>
    /// <param name="layout">Yerleşim.</param>
    /// <returns>Punto.</returns>
    public static int FontSizeFor(SubtitleLayout layout) =>
        layout.IsVertical
            ? (int)Math.Round(layout.Width * 0.0704)
            : (int)Math.Round(layout.Height * 0.0574);

    /// <summary>Yerleşime göre alt kenar boşluğu (px).</summary>
    /// <param name="layout">Yerleşim.</param>
    /// <returns>MarginV.</returns>
    public static int MarginVFor(SubtitleLayout layout) =>
        (int)Math.Round(layout.Height * (layout.IsVertical ? 0.22 : 0.12));

    /// <summary>ASS zaman biçimi (<c>h:mm:ss.cc</c>).</summary>
    /// <param name="ms">Milisaniye.</param>
    /// <returns>Metin.</returns>
    public static string FormatTime(int ms)
    {
        var cs = Math.Max(0, ms) / 10;
        var h = cs / 360_000;
        var m = cs / 6000 % 60;
        var s = cs / 100 % 60;
        var c = cs % 100;
        return string.Create(CultureInfo.InvariantCulture, $"{h}:{m:D2}:{s:D2}.{c:D2}");
    }

    /// <summary>Metni ASS için güvenli hale getirir (süslü parantez ve ters bölü override sayılmasın).</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Güvenli metin.</returns>
    public static string EscapeText(string text) =>
        text.Replace("\\", "/", StringComparison.Ordinal)
            .Replace("{", "(", StringComparison.Ordinal)
            .Replace("}", ")", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    /// <inheritdoc />
    protected override string Render(IReadOnlyList<CaptionLine> lines, SubtitleLayout layout)
    {
        var size = FontSizeFor(layout);
        var outline = Math.Max(2, (int)Math.Round(size * 0.08));
        var shadow = Math.Max(1, outline / 2);
        var marginH = (int)Math.Round(layout.Width * 0.06);
        var sb = new StringBuilder();
        sb.Append("[Script Info]\n")
          .Append("; Taha Konsept karaoke altyazı\n")
          .Append("ScriptType: v4.00+\n")
          .Append(CultureInfo.InvariantCulture, $"PlayResX: {layout.Width}\n")
          .Append(CultureInfo.InvariantCulture, $"PlayResY: {layout.Height}\n")
          .Append("WrapStyle: 0\n")
          .Append("ScaledBorderAndShadow: yes\n")
          .Append("YCbCr Matrix: TV.709\n\n")
          .Append("[V4+ Styles]\n")
          .Append("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding\n")
          .Append(CultureInfo.InvariantCulture, $"Style: {StyleName},{FontName},{size},&H0000C8FF,&H00FFFFFF,&H00101010,&H96000000,-1,0,0,0,100,100,0,0,1,{outline},{shadow},2,{marginH},{marginH},{MarginVFor(layout)},162\n\n")
          .Append("[Events]\n")
          .Append("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text\n");

        foreach (var line in lines)
        {
            sb.Append("Dialogue: 0,")
              .Append(FormatTime(line.StartMs)).Append(',')
              .Append(FormatTime(line.EndMs)).Append(',')
              .Append(StyleName).Append(",,0,0,0,,");
            for (var i = 0; i < line.Words.Count; i++)
            {
                var word = line.Words[i];
                var until = i + 1 < line.Words.Count ? line.Words[i + 1].StartMs : word.EndMs;
                var cs = Math.Max(1, (int)Math.Round((until - word.StartMs) / 10.0));
                if (i > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(CultureInfo.InvariantCulture, $"{{\\k{cs}}}").Append(EscapeText(word.Text));
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }
}
