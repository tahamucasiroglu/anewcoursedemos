using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Base;

/// <summary>
/// <see cref="IProfilePromptBuilder"/> ortak davranışı. Çıktı örneği:
/// <code>
/// Kanal: Bizans Günlükleri
/// Dil: Türkçe (tr); tüm metin ve seslendirme bu dilde.
/// Biçim: dikey 9:16; hedef ~60 sn.
/// Odak: 330–1453 Bizans; siyasi ve askerî dönüm noktaları…
/// Ton: merak uyandıran | Kitle: genel izleyici | Anlatım: hikâye
/// </code>
/// Boş alanlar yazılmaz; boşluklar tek boşluğa indirilir; kanal içeriği <see cref="MaxDescriptionLength"/> karakterde kelime sınırından kesilir.
/// </summary>
public abstract partial class ProfilePromptBuilderBase : IProfilePromptBuilder
{
    private static readonly Dictionary<string, string> LanguageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tr"] = "Türkçe",
        ["en"] = "İngilizce",
        ["de"] = "Almanca",
        ["fr"] = "Fransızca",
        ["es"] = "İspanyolca",
        ["it"] = "İtalyanca",
        ["pt"] = "Portekizce",
        ["ru"] = "Rusça",
        ["ar"] = "Arapça",
        ["fa"] = "Farsça",
        ["az"] = "Azerbaycan Türkçesi",
        ["ja"] = "Japonca",
        ["zh"] = "Çince",
        ["ko"] = "Korece",
        ["nl"] = "Felemenkçe",
        ["el"] = "Yunanca",
    };

    /// <summary>Kanal içeriğinin prompta girecek en fazla uzunluğu (karakter).</summary>
    protected virtual int MaxDescriptionLength => 1200;

    /// <summary>Ton/izleyici/anlatım alanlarının en fazla uzunluğu (karakter).</summary>
    protected virtual int MaxStyleLength => 80;

    /// <summary>Profilden sistem promptu parçası üretir.</summary>
    /// <param name="profile">Profil.</param>
    /// <returns>Kısa Türkçe metin.</returns>
    public virtual string Build(ProfileDto profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var sb = new StringBuilder(256);

        var name = Compact(profile.Name, 120);
        if (name.Length > 0)
        {
            sb.Append("Kanal: ").Append(name).Append('\n');
        }

        sb.Append("Dil: ").Append(DescribeLanguage(profile.Language)).Append("; tüm metin ve seslendirme bu dilde.\n");
        sb.Append("Biçim: ").Append(DescribeFormat(profile.Format)).Append("; hedef ").Append(DescribeDuration(profile.TargetDurationSec)).Append(".\n");

        var description = Compact(profile.ChannelDescription, MaxDescriptionLength);
        if (description.Length > 0)
        {
            sb.Append("Odak: ").Append(description).Append('\n');
        }

        var styles = new List<string>(3);
        AddStyle(styles, "Ton", profile.DefaultTone);
        AddStyle(styles, "Kitle", profile.DefaultAudience);
        AddStyle(styles, "Anlatım", profile.DefaultNarrativeStyle);
        if (styles.Count > 0)
        {
            sb.Append(string.Join(" | ", styles)).Append('\n');
        }

        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>Dil kodunu "Türkçe (tr)" biçimine çevirir; bilinmeyen kod olduğu gibi yazılır.</summary>
    /// <param name="language">Dil kodu.</param>
    /// <returns>Açıklama.</returns>
    protected virtual string DescribeLanguage(string? language)
    {
        var code = string.IsNullOrWhiteSpace(language) ? "tr" : language.Trim().ToLowerInvariant();
        var primary = code.Split('-')[0];
        return LanguageNames.TryGetValue(primary, out var label) ? $"{label} ({code})" : code;
    }

    /// <summary>Biçimi kısa Türkçe açıklamaya çevirir.</summary>
    /// <param name="format">Biçim.</param>
    /// <returns>Açıklama.</returns>
    protected virtual string DescribeFormat(VideoFormat format) =>
        format == VideoFormat.Horizontal ? "yatay 16:9" : "dikey 9:16";

    /// <summary>Süreyi "~60 sn" / "~3 dk" / "~2 dk 30 sn" biçimine çevirir.</summary>
    /// <param name="seconds">Saniye.</param>
    /// <returns>Açıklama.</returns>
    protected virtual string DescribeDuration(int seconds)
    {
        if (seconds < 120)
        {
            return string.Create(CultureInfo.InvariantCulture, $"~{seconds} sn");
        }

        var minutes = seconds / 60;
        var rest = seconds % 60;
        return rest == 0
            ? string.Create(CultureInfo.InvariantCulture, $"~{minutes} dk")
            : string.Create(CultureInfo.InvariantCulture, $"~{minutes} dk {rest} sn");
    }

    /// <summary>Metindeki tüm boşlukları (satır sonları dahil) tek boşluğa indirir ve gerekirse kelime sınırında kısaltır.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="maxLength">En fazla uzunluk.</param>
    /// <returns>Sıkıştırılmış metin (boşsa boş dize).</returns>
    protected static string Compact(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var compact = WhitespaceRegex().Replace(text, " ").Trim();
        if (compact.Length <= maxLength)
        {
            return compact;
        }

        var cut = compact[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > maxLength / 2)
        {
            cut = cut[..lastSpace];
        }

        return cut.TrimEnd(' ', ',', ';', '.', ':', '-') + "…";
    }

    private void AddStyle(List<string> target, string label, string? value)
    {
        var compact = Compact(value, MaxStyleLength);
        if (compact.Length > 0)
        {
            target.Add($"{label}: {compact}");
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
