using System.Globalization;
using Konsept.Modules.Profiles.Domain.Enums;

namespace Konsept.Modules.Profiles.Domain.Rules;

/// <summary>
/// Profil alan kuralları (spec 0003 Req 1–2): metinler kırpılır, uzunluk kırpılmış değerle ölçülür; video dili <c>tr|en</c>, biçim
/// <c>landscape|portrait</c>, hedef süre 15–900 sn (dahil). Ad tekilliği tr-TR küçük harf anahtarıyla ("İstanbul" = "istanbul").
/// </summary>
public static class ProfileRules
{
    /// <summary>Kullanıcı başına en fazla profil.</summary>
    public const int MaxProfiles = 20;

    /// <summary>Kanal adı üst sınırı.</summary>
    public const int NameMaxLength = 80;

    /// <summary>Kanal içeriği üst sınırı.</summary>
    public const int ContentMaxLength = 2000;

    /// <summary>En kısa hedef süre (sn).</summary>
    public const int MinDurationSeconds = 15;

    /// <summary>En uzun hedef süre (sn).</summary>
    public const int MaxDurationSeconds = 900;

    /// <summary>Video dili: Türkçe.</summary>
    public const string LanguageTurkish = "tr";

    /// <summary>Video dili: İngilizce.</summary>
    public const string LanguageEnglish = "en";

    /// <summary>Biçim: yatay.</summary>
    public const string FormatLandscape = "landscape";

    /// <summary>Biçim: dikey.</summary>
    public const string FormatPortrait = "portrait";

    /// <summary>Gets Türkçe kültür (sıralama ve tekillik).</summary>
    public static CultureInfo Turkish { get; } = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Gets izin verilen video dilleri (genişletilebilir liste, Req 2).</summary>
    public static IReadOnlyList<string> Languages { get; } = [LanguageTurkish, LanguageEnglish];

    /// <summary>Gets izin verilen biçimler.</summary>
    public static IReadOnlyList<string> Formats { get; } = [FormatLandscape, FormatPortrait];

    /// <summary>Gets kanal adına göre tr-TR alfabetik, büyük/küçük harf duyarsız karşılaştırıcı (Req 8).</summary>
    public static StringComparer NameOrder { get; } = StringComparer.Create(Turkish, CompareOptions.IgnoreCase);

    /// <summary>Kırpılmış metin (<c>null</c> → boş).</summary>
    /// <param name="value">Değer.</param>
    /// <returns>Kırpılmış değer.</returns>
    public static string Clean(string? value) => (value ?? string.Empty).Trim();

    /// <summary>Tekillik anahtarı: kırpılmış adın tr-TR küçük harfi.</summary>
    /// <param name="name">Kanal adı.</param>
    /// <returns>Anahtar.</returns>
    public static string NameKey(string? name) => Clean(name).ToLower(Turkish);

    /// <summary>Alanları denetler.</summary>
    /// <param name="name">Kanal adı.</param>
    /// <param name="language">Video dili.</param>
    /// <param name="format">Biçim.</param>
    /// <param name="durationSeconds">Hedef süre.</param>
    /// <param name="content">Kanal içeriği.</param>
    /// <returns>İhlaller (boşsa geçerli).</returns>
    public static IReadOnlyList<ProfileIssue> Check(string? name, string? language, string? format, int? durationSeconds, string? content)
    {
        var issues = new List<ProfileIssue>();
        var cleanName = Clean(name);
        if (cleanName.Length == 0)
        {
            issues.Add(ProfileIssue.NameRequired);
        }
        else if (cleanName.Length > NameMaxLength)
        {
            issues.Add(ProfileIssue.NameTooLong);
        }

        if (language is null || !Languages.Contains(language, StringComparer.Ordinal))
        {
            issues.Add(ProfileIssue.LanguageInvalid);
        }

        if (format is null || !Formats.Contains(format, StringComparer.Ordinal))
        {
            issues.Add(ProfileIssue.FormatInvalid);
        }

        if (durationSeconds is not (>= MinDurationSeconds and <= MaxDurationSeconds))
        {
            issues.Add(ProfileIssue.DurationOutOfRange);
        }

        var cleanContent = Clean(content);
        if (cleanContent.Length == 0)
        {
            issues.Add(ProfileIssue.ContentRequired);
        }
        else if (cleanContent.Length > ContentMaxLength)
        {
            issues.Add(ProfileIssue.ContentTooLong);
        }

        return issues;
    }
}
