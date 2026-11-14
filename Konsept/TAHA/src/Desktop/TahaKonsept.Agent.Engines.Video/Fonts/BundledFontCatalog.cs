using System.Globalization;
using System.Text;

namespace TahaKonsept.Agent.Engines.Video.Fonts;

/// <summary>
/// Sahnelerde kullanılabilecek paketlenmiş yazı tipleri (Türkçe karakterler için latin + latin-ext alt kümeleri) ve
/// bunları sahne klasörüne kopyalayıp <c>@font-face</c> CSS'i üreten yardımcılar.
/// Önemli üyeler: <see cref="Fonts"/>, <see cref="PackageVersion"/>, <see cref="CopyInto"/>, <see cref="BuildCss"/>.
/// </summary>
public static class BundledFontCatalog
{
    /// <summary>Fontsource paket sürümü (sabit).</summary>
    public const string PackageVersion = "5.3.0";

    /// <summary>Sahne klasöründe yazı tiplerinin bulunduğu alt klasör.</summary>
    public const string FontsFolder = "fonts";

    /// <summary>Üretilen CSS dosyasının adı (<see cref="FontsFolder"/> içinde).</summary>
    public const string CssFileName = "tk-fonts.css";

    private const string LatinRange = "U+0000-00FF,U+0131,U+0152-0153,U+02BB-02BC,U+02C6,U+02DA,U+02DC,U+0304,U+0308,U+0329,U+2000-206F,U+20AC,U+2122,U+2191,U+2193,U+2212,U+2215,U+FEFF,U+FFFD";

    private const string LatinExtRange = "U+0100-02BA,U+02BD-02C5,U+02C7-02CC,U+02CE-02D7,U+02DD-02FF,U+0304,U+0308,U+0329,U+1D00-1DBF,U+1E00-1E9F,U+1EF2-1EFF,U+2020,U+20A0-20AB,U+20AD-20C0,U+2113,U+2C60-2C7F,U+A720-A7FF";

    /// <summary>Paketlenen yazı tipleri.</summary>
    public static IReadOnlyList<BundledFont> Fonts { get; } =
    [
        new("Inter", "@fontsource/inter", "inter", [400, 700, 800]),
        new("Montserrat", "@fontsource/montserrat", "montserrat", [400, 700, 900]),
        new("Playfair Display", "@fontsource/playfair-display", "playfair-display", [400, 700]),
    ];

    /// <summary>Yazı tiplerini şablonun <c>node_modules</c> klasöründen hedef klasöre kopyalar ve CSS dosyasını yazar.</summary>
    /// <param name="templateDir">Motor şablon klasörü (içinde node_modules).</param>
    /// <param name="targetFontsDir">Hedef klasör (yoksa oluşturulur).</param>
    /// <param name="urlPrefix">CSS içindeki <c>url()</c> öneki (ör. boş = CSS ile aynı klasör).</param>
    /// <returns>Kopyalanan dosya sayısı.</returns>
    public static int CopyInto(string templateDir, string targetFontsDir, string urlPrefix = "")
    {
        Directory.CreateDirectory(targetFontsDir);
        var copied = 0;
        foreach (var font in Fonts)
        {
            var source = Path.Combine(templateDir, "node_modules", font.PackageName.Replace('/', Path.DirectorySeparatorChar), "files");
            foreach (var file in FileNames(font))
            {
                var from = Path.Combine(source, file);
                if (File.Exists(from))
                {
                    File.Copy(from, Path.Combine(targetFontsDir, file), overwrite: true);
                    copied++;
                }
            }
        }

        File.WriteAllText(Path.Combine(targetFontsDir, CssFileName), BuildCss(urlPrefix), new UTF8Encoding(false));
        return copied;
    }

    /// <summary>Tüm yazı tipleri için <c>@font-face</c> kurallarını üretir.</summary>
    /// <param name="urlPrefix">Dosya adlarının önüne eklenecek göreli yol.</param>
    /// <returns>CSS metni.</returns>
    public static string BuildCss(string urlPrefix = "")
    {
        var sb = new StringBuilder();
        foreach (var font in Fonts)
        {
            foreach (var weight in font.Weights)
            {
                AppendFace(sb, font, weight, "latin-ext", LatinExtRange, urlPrefix);
                AppendFace(sb, font, weight, "latin", LatinRange, urlPrefix);
            }
        }

        return sb.ToString();
    }

    /// <summary>Bir yazı tipinin kopyalanacak dosya adları.</summary>
    /// <param name="font">Yazı tipi.</param>
    /// <returns>Dosya adları.</returns>
    public static IEnumerable<string> FileNames(BundledFont font) =>
        font.Weights.SelectMany(w => new[] { FileName(font, "latin", w), FileName(font, "latin-ext", w) });

    private static string FileName(BundledFont font, string subset, int weight) =>
        string.Create(CultureInfo.InvariantCulture, $"{font.FilePrefix}-{subset}-{weight}-normal.woff2");

    private static void AppendFace(StringBuilder sb, BundledFont font, int weight, string subset, string range, string urlPrefix) =>
        sb.Append(CultureInfo.InvariantCulture,
            $"@font-face{{font-family:'{font.Family}';font-style:normal;font-display:block;font-weight:{weight};src:url('{urlPrefix}{FileName(font, subset, weight)}') format('woff2');unicode-range:{range};}}\n");
}
