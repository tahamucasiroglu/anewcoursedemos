namespace TahaKonsept.Modules.Diagnostics.Application.Common;

/// <summary>
/// Günlük/istatistiklerde "gün" Türkiye saatine (Europe/Istanbul, UTC+3) göredir. Önemli üyeler: <see cref="Zone"/>,
/// <see cref="ToLocal"/>, <see cref="LocalDate"/>, <see cref="DayRangeUtc"/>, <see cref="LastDays"/>.
/// </summary>
public static class DiagnosticsTime
{
    /// <summary>Türkiye saat dilimi (bulunamazsa sabit UTC+3).</summary>
    public static TimeZoneInfo Zone { get; } = ResolveZone();

    /// <summary>UTC anını Türkiye saatine çevirir.</summary>
    /// <param name="utc">An.</param>
    /// <returns>Yerel an.</returns>
    public static DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Zone);

    /// <summary>Anın Türkiye saatine göre günü.</summary>
    /// <param name="utc">An.</param>
    /// <returns>Gün.</returns>
    public static DateOnly LocalDate(DateTimeOffset utc) => DateOnly.FromDateTime(ToLocal(utc).DateTime);

    /// <summary>Günün UTC aralığı: [başlangıç, ertesi gün başlangıcı).</summary>
    /// <param name="day">Gün (Türkiye saati).</param>
    /// <returns>Aralık.</returns>
    public static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) DayRangeUtc(DateOnly day)
    {
        var start = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var end = day.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return (new DateTimeOffset(start, Zone.GetUtcOffset(start)).ToUniversalTime(),
            new DateTimeOffset(end, Zone.GetUtcOffset(end)).ToUniversalTime());
    }

    /// <summary>Bugün dahil son <paramref name="count"/> gün (eskiden yeniye).</summary>
    /// <param name="nowUtc">Şu an.</param>
    /// <param name="count">Gün sayısı.</param>
    /// <returns>Günler.</returns>
    public static IReadOnlyList<DateOnly> LastDays(DateTimeOffset nowUtc, int count)
    {
        var today = LocalDate(nowUtc);
        return Enumerable.Range(0, Math.Max(count, 0)).Select(i => today.AddDays(i - count + 1)).ToList();
    }

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
            {
                return zone;
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("TR+3", TimeSpan.FromHours(3), "Türkiye", "Türkiye");
    }
}
