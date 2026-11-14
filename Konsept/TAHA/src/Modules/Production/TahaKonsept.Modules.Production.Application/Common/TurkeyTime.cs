namespace TahaKonsept.Modules.Production.Application.Common;

/// <summary>
/// Türkiye saati yardımcıları (günlük istatistik ve işlem günlüğü metinleri için). Saat dilimi bulunamazsa sabit UTC+3 kullanılır.
/// </summary>
public static class TurkeyTime
{
    /// <summary>Türkiye saat dilimi.</summary>
    public static TimeZoneInfo Zone { get; } = Resolve();

    /// <summary>UTC anını Türkiye saatine çevirir.</summary>
    /// <param name="utc">UTC an.</param>
    /// <returns>Yerel an.</returns>
    public static DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Zone);

    /// <summary>UTC anının Türkiye saatine göre günü.</summary>
    /// <param name="utc">UTC an.</param>
    /// <returns>Gün.</returns>
    public static DateOnly DayOf(DateTimeOffset utc) => DateOnly.FromDateTime(ToLocal(utc).DateTime);

    /// <summary>Türkiye saatine göre günün başlangıcının UTC karşılığı.</summary>
    /// <param name="day">Gün.</param>
    /// <returns>UTC an.</returns>
    public static DateTimeOffset StartOfDayUtc(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }

    private static TimeZoneInfo Resolve()
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
