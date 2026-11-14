using Shouldly;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Modules.Providers.Domain.Entities;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence;
using TahaKonsept.Modules.Providers.Infrastructure.Stats.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Stats;

/// <summary>
/// Yönetim paneli harcama katkıcısı: bu ayki toplam (tüm kullanıcılar, Türkiye saati) ve günlük seri.
/// </summary>
public sealed class ProvidersStatsContributorTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    private readonly FixedClock _clock = new(Now);
    private readonly ProvidersDbContext _db;

    /// <summary>Kurucu.</summary>
    public ProvidersStatsContributorTests()
    {
        _db = TestFixtures.Db(_clock);
    }

    /// <summary>Ay toplamı ve gün serisi Türkiye saatine göre hesaplanır; tüm kullanıcılar dahil.</summary>
    [Fact]
    public async Task Month_total_and_daily_series_use_turkey_time()
    {
        var ct = TestContext.Current.CancellationToken;
        await AddAsync(Guid.NewGuid(), 1.00m, new DateTimeOffset(2026, 11, 14, 8, 0, 0, TimeSpan.Zero), ct);
        await AddAsync(Guid.NewGuid(), 0.50m, new DateTimeOffset(2026, 11, 13, 22, 30, 0, TimeSpan.Zero), ct); // 14 Kasım 01:30 TR
        await AddAsync(Guid.NewGuid(), 2.00m, new DateTimeOffset(2026, 10, 31, 22, 0, 0, TimeSpan.Zero), ct);  // 1 Kasım 01:00 TR
        await AddAsync(Guid.NewGuid(), 4.00m, new DateTimeOffset(2026, 10, 31, 20, 0, 0, TimeSpan.Zero), ct);  // 31 Ekim 23:00 TR

        var stats = await new ProvidersStatsContributor(_db, _clock).GetStatsAsync(ct);

        stats[StatsKeys.SpendMonthUsd].ShouldBe(3.50m);
        var series = stats[StatsKeys.SpendPerDayUsd].ShouldBeAssignableTo<IReadOnlyDictionary<DateOnly, decimal>>()!;
        series[new DateOnly(2026, 11, 14)].ShouldBe(1.50m);
        series[new DateOnly(2026, 11, 1)].ShouldBe(2.00m);
        series[new DateOnly(2026, 10, 31)].ShouldBe(4.00m);
    }

    /// <summary>Kayıt yoksa sıfır ve boş seri.</summary>
    [Fact]
    public async Task Empty_log_gives_zero()
    {
        var stats = await new ProvidersStatsContributor(_db, _clock).GetStatsAsync(TestContext.Current.CancellationToken);

        stats[StatsKeys.SpendMonthUsd].ShouldBe(0m);
        stats[StatsKeys.SpendPerDayUsd].ShouldBeAssignableTo<IReadOnlyDictionary<DateOnly, decimal>>()!.ShouldBeEmpty();
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _db.Dispose();

    /// <summary>Belirli zamanda çağrı kaydı ekler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cost">Maliyet.</param>
    /// <param name="at">Zaman.</param>
    /// <param name="ct">İptal belirteci.</param>
    private async Task AddAsync(Guid userId, decimal cost, DateTimeOffset at, CancellationToken ct)
    {
        _clock.UtcNow = at;
        _db.CallLogs.Add(CallLog.Create(userId, "anthropic", "m", "m", "concept", 1, 1, 0, 0, 0, null, cost, 10, true, null, null));
        await _db.SaveChangesAsync(ct);
        _clock.UtcNow = Now;
    }
}
