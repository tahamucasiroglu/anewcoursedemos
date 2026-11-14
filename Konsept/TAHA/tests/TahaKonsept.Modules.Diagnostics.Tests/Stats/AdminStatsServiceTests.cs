using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Modules.Diagnostics.Application.Concrete;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence.Repositories;
using TahaKonsept.Modules.Diagnostics.Tests.Support;
using TahaKonsept.Shared.Contracts.Diagnostics;
using Xunit;

namespace TahaKonsept.Modules.Diagnostics.Tests.Stats;

/// <summary>
/// <see cref="AdminStatsService"/>: katkıcıların birleştirilmesi, hatalı katkıcının atlanması, 30 günlük seriler, yetki,
/// ve <see cref="DiagnosticsStatsContributor"/>'ın kendi sayıları.
/// </summary>
public sealed class AdminStatsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);
    private readonly FakeClock _clock = new(Now);
    private readonly Guid _admin = Guid.NewGuid();

    /// <summary>Farklı modüllerin değerleri tek yanıtta tipli alanlara düşer; tanınmayan basit değerler Extra'ya gider.</summary>
    [Fact]
    public async Task Aggregates_all_contributors()
    {
        var today = new DateOnly(2026, 11, 14);
        var production = Contributor(new Dictionary<string, object>
        {
            [StatsKeys.VideosTotal] = 42L,
            [StatsKeys.VideosPerDay] = new Dictionary<DateOnly, int> { [today] = 3, [today.AddDays(-1)] = 2, [today.AddDays(-45)] = 9 },
            ["production.queueLength"] = 4,
        });
        var providers = Contributor(new Dictionary<string, object>
        {
            [StatsKeys.SpendMonthUsd] = 12.5m,
            [StatsKeys.SpendPerDayUsd] = new List<DailyValue> { new() { Date = today, Value = 1.25m } },
            ["providers.complex"] = new object(),
        });
        var concepts = Contributor(new Dictionary<string, object> { [StatsKeys.ConceptsTotal] = 17, [StatsKeys.UsersTotal] = 1 });

        var result = await Service(TestUsers.Admin(_admin), production, providers, concepts).GetAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var stats = result.Value;
        stats.TotalVideos.ShouldBe(42);
        stats.TotalConcepts.ShouldBe(17);
        stats.UserCount.ShouldBe(1);
        stats.MonthSpendUsd.ShouldBe(12.5m);
        stats.VideosPerDay.Count.ShouldBe(AdminStatsService.SeriesDays);
        stats.VideosPerDay[^1].ShouldBe(new DailyValue { Date = today, Value = 3 });
        stats.VideosPerDay[^2].Value.ShouldBe(2);
        stats.VideosPerDay.Sum(d => d.Value).ShouldBe(5); // 45 gün önceki dışarıda
        stats.VideosPerDay[0].Date.ShouldBe(today.AddDays(-29));
        stats.SpendPerDayUsd[^1].Value.ShouldBe(1.25m);
        stats.Extra["production.queueLength"].ShouldBe(4);
        stats.Extra.ContainsKey("providers.complex").ShouldBeFalse();
        stats.ActivityByLevel.Select(l => l.Level).ShouldBe([LogLevelKind.Information, LogLevelKind.Warning, LogLevelKind.Error, LogLevelKind.Critical]);
        stats.FailedContributors.ShouldBeEmpty();
    }

    /// <summary>Aynı anahtarı veren iki katkıcıdan sonra kaydedilen kazanır.</summary>
    [Fact]
    public async Task Later_contributor_overrides_same_key()
    {
        var first = Contributor(new Dictionary<string, object> { [StatsKeys.UsersTotal] = 0 });
        var second = Contributor(new Dictionary<string, object> { [StatsKeys.UsersTotal] = 3 });

        var result = await Service(TestUsers.Admin(_admin), first, second).GetAsync(TestContext.Current.CancellationToken);

        result.Value.UserCount.ShouldBe(3);
    }

    /// <summary>Hata veren katkıcı atlanır ve raporlanır; diğerleri gösterilir.</summary>
    [Fact]
    public async Task Failing_contributor_is_skipped_and_reported()
    {
        var broken = Substitute.For<IStatsContributor>();
        broken.GetStatsAsync(Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyDictionary<string, object>>>(_ => throw new InvalidOperationException("DB yok"));
        var healthy = Contributor(new Dictionary<string, object> { [StatsKeys.VideosTotal] = 5 });

        var result = await Service(TestUsers.Admin(_admin), broken, healthy).GetAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalVideos.ShouldBe(5);
        result.Value.FailedContributors.ShouldHaveSingleItem();
    }

    /// <summary>Yönetici olmayan kullanıcı 403 alır, katkıcılar çağrılmaz.</summary>
    [Fact]
    public async Task Non_admin_is_forbidden()
    {
        var contributor = Substitute.For<IStatsContributor>();

        var result = await Service(TestUsers.Regular(Guid.NewGuid()), contributor).GetAsync(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        await contributor.DidNotReceive().GetStatsAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Diagnostics katkısı: açık bildirim, toplam kayıt, son 30 gün seviye dağılımı, dizin bağlıysa kullanıcı sayısı.</summary>
    [Fact]
    public async Task Diagnostics_contributor_reports_own_counts()
    {
        await using var db = TestDb.Create(_clock);
        _clock.Advance(TimeSpan.FromDays(-40));
        db.ActivityLogs.Add(ActivityLog.Create(Guid.Empty, LogLevelKind.Error, "Test", "eski hata"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _clock.Advance(TimeSpan.FromDays(40));
        db.ActivityLogs.AddRange(
            ActivityLog.Create(Guid.Empty, LogLevelKind.Information, "Test", "a"),
            ActivityLog.Create(Guid.Empty, LogLevelKind.Information, "Test", "b"),
            ActivityLog.Create(Guid.Empty, LogLevelKind.Warning, "Test", "c"));
        var resolved = ErrorReport.Create(Guid.Empty, ErrorReportSource.Web, "çözüldü", null, null, null, null, null);
        resolved.ChangeStatus(ErrorReportStatus.Resolved, Now);
        db.ErrorReports.AddRange(
            ErrorReport.Create(Guid.Empty, ErrorReportSource.Web, "yeni", null, null, null, null, null),
            resolved);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var directory = Substitute.For<IAdminUserDirectory>();
        directory.IsAvailable.Returns(true);
        directory.ListAsync(Arg.Any<CancellationToken>()).Returns([new AdminUserDto { Id = _admin, UserName = "taha" }]);
        var contributor = new DiagnosticsStatsContributor(new ActivityLogRepository(db), new ErrorReportRepository(db), directory, _clock);

        var result = await Service(TestUsers.Admin(_admin), contributor).GetAsync(TestContext.Current.CancellationToken);

        var stats = result.Value;
        stats.OpenErrorReports.ShouldBe(1);
        stats.TotalActivityLogs.ShouldBe(4);
        stats.UserCount.ShouldBe(1);
        stats.ActivityByLevel.Single(l => l.Level == LogLevelKind.Information).Count.ShouldBe(2);
        stats.ActivityByLevel.Single(l => l.Level == LogLevelKind.Warning).Count.ShouldBe(1);
        stats.ActivityByLevel.Single(l => l.Level == LogLevelKind.Error).Count.ShouldBe(0); // 40 gün önceki sayılmaz
    }

    private AdminStatsService Service(TahaKonsept.BuildingBlocks.Application.Abstract.ICurrentUser user, params IStatsContributor[] contributors) =>
        new(contributors, _clock, user, NullLogger<AdminStatsService>.Instance);

    private static IStatsContributor Contributor(IReadOnlyDictionary<string, object> values)
    {
        var contributor = Substitute.For<IStatsContributor>();
        contributor.GetStatsAsync(Arg.Any<CancellationToken>()).Returns(values);
        return contributor;
    }
}
