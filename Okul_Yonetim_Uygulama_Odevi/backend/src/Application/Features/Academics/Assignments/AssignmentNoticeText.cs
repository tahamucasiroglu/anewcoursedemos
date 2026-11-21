using System.Globalization;
using System.Text.RegularExpressions;

namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>
/// Yeni ödev bildiriminin metni (spec 0013 gereksinim 2): YAML şablonundaki <c>{subject}</c>, <c>{title}</c>, <c>{due}</c>
/// (gün.ay.yıl) yer tutucuları şablon üzerinde tek geçişte doldurulur — ders adında ya da başlıkta yazan <c>{title}</c> gibi bir
/// metin yeniden genişlemez, metin sınırlar içinde kalır (review round 1, R1-2; Communication'daki <c>GuardianNoticeEmail</c> ile aynı kalıp).
/// </summary>
public static partial class AssignmentNoticeText
{
    public static string Render(string template, string subject, string title, DateOnly due)
    {
        ArgumentNullException.ThrowIfNull(template);
        string day = due.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        return Placeholder().Replace(template, m => m.Groups[1].Value switch
        {
            "subject" => subject,
            "title" => title,
            _ => day,
        });
    }

    [GeneratedRegex(@"\{(subject|title|due)\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
}
