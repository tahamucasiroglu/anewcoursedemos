using System.Globalization;
using Konsept.Modules.Monitoring.Domain.Enums;

namespace Konsept.Modules.Monitoring.Domain.Rules;

/// <summary>
/// Günlük gün kuralları (spec 0021 Req 5, 6, 8): kayıtlar Europe/Istanbul saatine göre gece yarısında günlere ayrılır; zamanlar UTC
/// saklanır. İstenen gün geçersiz ya da bugünden ileriyse bugün, gezilebilecek ilk günden önceyse ilk gün olur.
/// </summary>
public static class JournalDay
{
    /// <summary>Gösterim saat dilimi (IANA).</summary>
    public const string TimeZoneId = "Europe/Istanbul";

    /// <summary>Tarih biçimi (adres ve cevapta).</summary>
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>Geçmişte bir seferde gösterilen kayıt sayısı.</summary>
    public const int PageSize = 50;

    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    /// <summary>Bir UTC anının Europe/Istanbul'daki günü.</summary>
    /// <param name="instant">An.</param>
    /// <returns>Gün.</returns>
    public static DateOnly DayOf(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);

    /// <summary>Bir günün UTC aralığı: <c>[başlangıç, bitiş)</c>.</summary>
    /// <param name="day">Gün (Europe/Istanbul).</param>
    /// <returns>Aralık.</returns>
    public static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) RangeOf(DateOnly day) => (StartOf(day), StartOf(day.AddDays(1)));

    /// <summary>İstenen günü kurallara göre çözer.</summary>
    /// <param name="requested">İstenen gün (<see cref="DateFormat"/>); boş ya da geçersizse bugün.</param>
    /// <param name="today">Bugün (Europe/Istanbul).</param>
    /// <param name="firstDay">Gezilebilecek ilk gün; bugünden sonraysa bugün sayılır.</param>
    /// <returns>Gösterilecek gün.</returns>
    public static DateOnly Resolve(string? requested, DateOnly today, DateOnly firstDay)
    {
        var first = firstDay > today ? today : firstDay;
        if (string.IsNullOrWhiteSpace(requested)
            || !DateOnly.TryParseExact(requested.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            || day > today)
        {
            return today;
        }

        return day < first ? first : day;
    }

    /// <summary>Filtre metnini çözer (<c>all</c>, <c>warning</c>, <c>error</c>); bilinmeyen değer Tümü'dür.</summary>
    /// <param name="value">Metin.</param>
    /// <returns>Filtre.</returns>
    public static JournalFilter ParseFilter(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "warning" => JournalFilter.Warning,
        "error" => JournalFilter.Error,
        _ => JournalFilter.All,
    };

    /// <summary>Filtrenin kapsadığı seviyeler.</summary>
    /// <param name="filter">Filtre.</param>
    /// <returns>Seviyeler; Tümü için <c>null</c> (süzme yok).</returns>
    public static IReadOnlyList<EventLevel>? LevelsOf(JournalFilter filter) => filter switch
    {
        JournalFilter.Warning => [EventLevel.Warning],
        JournalFilter.Error => [EventLevel.Error, EventLevel.Critical],
        _ => null,
    };

    private static DateTimeOffset StartOf(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
