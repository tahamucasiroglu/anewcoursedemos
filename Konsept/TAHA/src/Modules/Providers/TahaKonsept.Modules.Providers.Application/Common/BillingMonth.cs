using System.Globalization;

namespace TahaKonsept.Modules.Providers.Application.Common;

/// <summary>
/// Faturalama ayı hesabı: ay sınırları Türkiye saatine (<c>Europe/Istanbul</c>, UTC+3) göre; sonuç UTC aralığı.
/// Önemli üyeler: <see cref="For"/>.
/// </summary>
public static class BillingMonth
{
    /// <summary>Türkiye saat dilimi (bulunamazsa sabit UTC+3).</summary>
    private static readonly TimeZoneInfo Zone = ResolveZone();

    /// <summary>Verilen anın ayını döner.</summary>
    /// <param name="nowUtc">An (UTC).</param>
    /// <returns>Etiket (<c>yyyy-MM</c>), başlangıç (dahil) ve bitiş (hariç), UTC.</returns>
    public static (string Label, DateTimeOffset StartUtc, DateTimeOffset EndUtc) For(DateTimeOffset nowUtc)
    {
        var local = TimeZoneInfo.ConvertTime(nowUtc, Zone);
        var startLocal = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddMonths(1);
        var start = new DateTimeOffset(startLocal, Zone.GetUtcOffset(startLocal)).ToUniversalTime();
        var end = new DateTimeOffset(endLocal, Zone.GetUtcOffset(endLocal)).ToUniversalTime();
        return (startLocal.ToString("yyyy-MM", CultureInfo.InvariantCulture), start, end);
    }

    /// <summary>Saat dilimini çözer.</summary>
    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("TR", TimeSpan.FromHours(3), "Türkiye", "Türkiye");
        }
    }
}
