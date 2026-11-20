using System.Globalization;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication.WorkingHours;

/// <summary>
/// Mesai saatlerinin API biçimi (spec 0011): saat "SS:DD" (24 saat, değişmez kültür, 15 dakikalık adım) ve gün adları
/// (<c>Monday</c>…<c>Sunday</c>, Pazartesi'den başlayan sırayla).
/// </summary>
public static class QuietHoursText
{
    private static readonly WorkDays[] Week =
        [WorkDays.Monday, WorkDays.Tuesday, WorkDays.Wednesday, WorkDays.Thursday, WorkDays.Friday, WorkDays.Saturday, WorkDays.Sunday];

    /// <summary>"SS:DD" biçiminde ve 15 dakikalık adımda mı.</summary>
    public static bool TryParseTime(string? text, out TimeOnly time) =>
        TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time) && QuietHours.IsOnStep(time);

    public static string Format(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Gün adları geçerli mi (boş liste <see cref="WorkDays.None"/>); birleşik bayrak döner.</summary>
    public static bool TryParseDays(IEnumerable<string>? names, out WorkDays days)
    {
        days = WorkDays.None;
        foreach (string name in names ?? [])
        {
            WorkDays day = Week.FirstOrDefault(d => d.ToString() == name);
            if (day == WorkDays.None)
            {
                return false;
            }

            days |= day;
        }

        return true;
    }

    public static IReadOnlyList<string> DayNames(WorkDays days) => [.. Week.Where(d => (days & d) != WorkDays.None).Select(d => d.ToString())];
}
