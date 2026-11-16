using System.Globalization;

namespace Konsept.Modules.Production.Domain.Rules;

/// <summary>
/// Günlük olaylarındaki süre ve saat metni (spec 0017 Req 18, AC-21): süre "57 sn", "30 dk 12 sn", "1 sa 5 dk 3 sn"; saat aralığı
/// Europe/Istanbul'da <c>HH:mm–HH:mm</c>.
/// </summary>
public static class JobTimeText
{
    /// <summary>Saat dilimi.</summary>
    public const string TimeZoneId = "Europe/Istanbul";

    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    /// <summary>Süre metni.</summary>
    /// <param name="duration">Süre.</param>
    /// <returns>Metin.</returns>
    public static string Duration(TimeSpan duration)
    {
        var total = (long)Math.Max(0, Math.Round(duration.TotalSeconds));
        var (h, m, s) = (total / 3600, total % 3600 / 60, total % 60);
        var c = CultureInfo.InvariantCulture;
        return h > 0 ? string.Format(c, "{0} sa {1} dk {2} sn", h, m, s)
            : m > 0 ? string.Format(c, "{0} dk {1} sn", m, s)
            : string.Format(c, "{0} sn", s);
    }

    /// <summary>Saat aralığı (İstanbul).</summary>
    /// <param name="start">Başlangıç (UTC).</param>
    /// <param name="end">Bitiş (UTC).</param>
    /// <returns>Metin, ör. "10:30–11:00".</returns>
    public static string Range(DateTimeOffset start, DateTimeOffset end) =>
        Clock(start) + "–" + Clock(end);

    private static string Clock(DateTimeOffset at) => TimeZoneInfo.ConvertTime(at, Zone).ToString("HH:mm", CultureInfo.InvariantCulture);
}
