using System.Globalization;

namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

/// <summary>
/// Görsel boyutu yardımcıları: istenen genişlik/yüksekliğe en yakın en-boy oranı (<see cref="NearestAspectRatio"/>; Gemini,
/// Replicate), en yakın sabit boyut (<see cref="NearestSize"/>; OpenAI) ve katına yuvarlayıp piksel sınırına sığdırma
/// (<see cref="Fit"/>; BFL, Z.ai, fal).
/// </summary>
public static class ImageSizes
{
    /// <summary>Gemini / Replicate'in desteklediği en-boy oranları.</summary>
    public static IReadOnlyList<string> CommonAspectRatios { get; } = ["1:1", "2:3", "3:2", "3:4", "4:3", "4:5", "5:4", "9:16", "16:9", "21:9"];

    /// <summary>İstenen orana en yakın oranı seçer (logaritmik uzaklık).</summary>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <param name="supported">Desteklenen oranlar (<c>G:Y</c>).</param>
    public static string NearestAspectRatio(int width, int height, IReadOnlyList<string> supported)
    {
        ArgumentNullException.ThrowIfNull(supported);
        var target = Math.Log((double)width / height);
        return supported.MinBy(r => Math.Abs(Math.Log(RatioOf(r)) - target)) ?? "1:1";
    }

    /// <summary>İstenen orana en yakın sabit boyutu seçer (ör. OpenAI <c>1024x1536</c>).</summary>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <param name="supported">Desteklenen boyutlar (<c>GxY</c>).</param>
    public static string NearestSize(int width, int height, IReadOnlyList<string> supported)
    {
        ArgumentNullException.ThrowIfNull(supported);
        var target = Math.Log((double)width / height);
        return supported.MinBy(s => Math.Abs(Math.Log(RatioOf(s.Replace('x', ':'))) - target)) ?? "1024x1024";
    }

    /// <summary>Boyutları katına yuvarlar ve toplam piksel sınırını aşıyorsa oranı koruyarak küçültür.</summary>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <param name="multiple">Kat (ör. 16, 32).</param>
    /// <param name="maxPixels">En fazla piksel (0 = sınırsız).</param>
    public static (int Width, int Height) Fit(int width, int height, int multiple, long maxPixels = 0)
    {
        double w = width;
        double h = height;
        if (maxPixels > 0 && w * h > maxPixels)
        {
            var scale = Math.Sqrt(maxPixels / (w * h));
            w *= scale;
            h *= scale;
        }

        var fw = Math.Max(multiple, (int)Math.Round(w / multiple) * multiple);
        var fh = Math.Max(multiple, (int)Math.Round(h / multiple) * multiple);
        while (maxPixels > 0 && (long)fw * fh > maxPixels)
        {
            if (fw >= fh)
            {
                fw -= multiple;
            }
            else
            {
                fh -= multiple;
            }
        }

        return (fw, fh);
    }

    /// <summary><c>G:Y</c> metninden oran.</summary>
    /// <param name="ratio">Oran metni.</param>
    private static double RatioOf(string ratio)
    {
        var parts = ratio.Split(':');
        return double.Parse(parts[0], CultureInfo.InvariantCulture) / double.Parse(parts[1], CultureInfo.InvariantCulture);
    }
}
