using System.Globalization;
using System.Text.RegularExpressions;

namespace OkulYonetim.Application.Features.Appointments;

/// <summary>
/// Randevu bildiriminin metni (spec 0016 gereksinim 7): YAML şablonundaki <c>{teacher}</c>, <c>{day}</c> (gün.ay.yıl), <c>{time}</c>
/// (SS:DD, Türkiye saati) ve <c>{reason}</c> yer tutucuları şablon üzerinde <b>tek geçişte</b> doldurulur — öğretmen adında ya da
/// nedende yazan <c>{day}</c> gibi bir metin yeniden genişlemez (0013 review R1-2 kalıbı). Konu metni hiçbir şablonda yoktur.
/// </summary>
public static partial class AppointmentNoticeText
{
    public static string Render(string template, string teacher, DateTimeOffset turkeyStart, string? reason)
    {
        ArgumentNullException.ThrowIfNull(template);
        string day = turkeyStart.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        string time = turkeyStart.ToString("HH:mm", CultureInfo.InvariantCulture);
        return Placeholder().Replace(template, m => m.Groups[1].Value switch
        {
            "teacher" => teacher,
            "day" => day,
            "time" => time,
            _ => reason ?? string.Empty,
        });
    }

    [GeneratedRegex(@"\{(teacher|day|time|reason)\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
}
