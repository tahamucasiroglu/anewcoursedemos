using System.Globalization;

namespace TahaKonsept.Modules.Diagnostics.Application.Common;

/// <summary>
/// Süreli işlem mesajı biçimleyici: <see cref="Format"/> ("57 sn", "3 dk 12 sn", "1 sa 5 dk") ve
/// <see cref="Describe"/> ("Video hazırlandı — süre 57 sn, başlangıç 10:30, bitiş 11:00"; saatler Türkiye saati).
/// </summary>
public static class ActivityDurationText
{
    /// <summary>Süreyi kısa Türkçe metne çevirir.</summary>
    /// <param name="duration">Süre (negatifse 0 kabul edilir).</param>
    /// <returns>Metin.</returns>
    public static string Format(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        if (duration.TotalSeconds < 1)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{(int)duration.TotalMilliseconds} ms");
        }

        if (duration.TotalMinutes < 1)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{(int)duration.TotalSeconds} sn");
        }

        if (duration.TotalHours < 1)
        {
            return duration.Seconds == 0
                ? string.Create(CultureInfo.InvariantCulture, $"{duration.Minutes} dk")
                : string.Create(CultureInfo.InvariantCulture, $"{duration.Minutes} dk {duration.Seconds} sn");
        }

        var hours = (int)duration.TotalHours;
        return duration.Minutes == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{hours} sa")
            : string.Create(CultureInfo.InvariantCulture, $"{hours} sa {duration.Minutes} dk");
    }

    /// <summary>Mesajın sonuna süre, başlangıç ve bitiş saatini ekler.</summary>
    /// <param name="message">Mesaj.</param>
    /// <param name="startedAtUtc">Başlangıç.</param>
    /// <param name="finishedAtUtc">Bitiş.</param>
    /// <returns>Metin.</returns>
    public static string Describe(string message, DateTimeOffset startedAtUtc, DateTimeOffset finishedAtUtc)
    {
        var start = DiagnosticsTime.ToLocal(startedAtUtc);
        var end = DiagnosticsTime.ToLocal(finishedAtUtc);
        var timeFormat = (end - start).TotalMinutes < 1 ? "HH:mm:ss" : "HH:mm";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{message?.Trim()} — süre {Format(finishedAtUtc - startedAtUtc)}, başlangıç {start.ToString(timeFormat, CultureInfo.InvariantCulture)}, bitiş {end.ToString(timeFormat, CultureInfo.InvariantCulture)}");
    }
}
