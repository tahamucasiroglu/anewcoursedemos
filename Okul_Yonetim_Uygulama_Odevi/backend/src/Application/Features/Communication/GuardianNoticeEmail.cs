using System.Globalization;
using System.Text.RegularExpressions;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Devamsızlık/düzeltme bildirimi metni ve e-postası (spec 0010 gereksinim 6, 7, 10). Şablonlar YAML'dan gelir; yer tutucular
/// (<c>{student}</c>, <c>{date}</c> = gün.ay.yıl, <c>{school}</c>) tek geçişte doldurulur — addaki süslü parantez başka bir yer
/// tutucuya dönüşmez. Gövde yalnızca öğrenci adı, gün, okul adı ve durumu içerir (sağlık, sınıf arkadaşı, öğretmen notu yok);
/// alıcının adı yalnızca e-posta başlığındadır. E-posta loglanmaz (spec 0006).
/// </summary>
public static partial class GuardianNoticeEmail
{
    public static string Render(string template, string studentName, DateOnly day, string schoolName)
    {
        ArgumentNullException.ThrowIfNull(template);
        string date = day.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        return Placeholder().Replace(template, m => m.Groups[1].Value switch
        {
            "student" => studentName,
            "date" => date,
            _ => schoolName,
        });
    }

    public static EmailMessage Build(GuardianContact to, string subject, string text, string footer)
    {
        ArgumentNullException.ThrowIfNull(to);
        return new EmailMessage(to.Email, to.FullName, subject, text + "\n\n" + footer + "\n", to.UserId);
    }

    [GeneratedRegex(@"\{(student|date|school)\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
}
