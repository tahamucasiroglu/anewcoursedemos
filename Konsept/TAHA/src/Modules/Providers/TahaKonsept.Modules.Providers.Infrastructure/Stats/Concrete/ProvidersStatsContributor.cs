using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence;

namespace TahaKonsept.Modules.Providers.Infrastructure.Stats.Concrete;

/// <summary>
/// Yönetim paneli için Providers katkıcısı (Diagnostics <c>IStatsContributor</c>): tüm kullanıcıların bu ayki harcaması
/// (<see cref="StatsKeys.SpendMonthUsd"/>, ay sınırı Türkiye saati) ve son <see cref="SeriesDays"/> günün günlük harcama serisi
/// (<see cref="StatsKeys.SpendPerDayUsd"/>, Türkiye saatine göre gün → $). Kayıt: <c>TryAddEnumerable(Scoped&lt;IStatsContributor, ProvidersStatsContributor&gt;)</c>.
/// </summary>
public sealed class ProvidersStatsContributor : StatsContributorBase
{
    /// <summary>Günlük serinin gün sayısı.</summary>
    public const int SeriesDays = 30;

    private static readonly TimeZoneInfo Zone = ResolveZone();

    private readonly ProvidersDbContext _db;
    private readonly IClock _clock;

    /// <summary>Oluşturur.</summary>
    /// <param name="db">Providers DbContext.</param>
    /// <param name="clock">Saat.</param>
    public ProvidersStatsContributor(ProvidersDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Harcama değerlerini sözlüğe yazar.</summary>
    /// <param name="stats">Yazılacak sözlük.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected override async Task ContributeAsync(IDictionary<string, object> stats, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stats);
        var now = _clock.UtcNow;
        var month = BillingMonth.For(now);
        var todayLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Zone).DateTime);
        var seriesStartLocal = todayLocal.AddDays(-(SeriesDays - 1)).ToDateTime(TimeOnly.MinValue);
        var seriesStartUtc = new DateTimeOffset(seriesStartLocal, Zone.GetUtcOffset(seriesStartLocal)).ToUniversalTime();
        var fromUtc = seriesStartUtc < month.StartUtc ? seriesStartUtc : month.StartUtc;

        var rows = await _db.CallLogs.AsNoTracking()
            .Where(c => c.CreatedAtUtc >= fromUtc && c.CostUsd > 0)
            .Select(c => new { c.CreatedAtUtc, c.CostUsd })
            .ToListAsync(cancellationToken);

        stats[StatsKeys.SpendMonthUsd] = rows.Where(r => r.CreatedAtUtc >= month.StartUtc && r.CreatedAtUtc < month.EndUtc).Sum(r => r.CostUsd);
        stats[StatsKeys.SpendPerDayUsd] = rows
            .Where(r => r.CreatedAtUtc >= seriesStartUtc)
            .GroupBy(r => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(r.CreatedAtUtc, Zone).DateTime))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.CostUsd));
    }

    /// <summary>Türkiye saat dilimini çözer (bulunamazsa sabit UTC+3).</summary>
    /// <returns>Saat dilimi.</returns>
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
