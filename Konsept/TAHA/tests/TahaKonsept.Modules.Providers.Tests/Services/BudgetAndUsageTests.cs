using Shouldly;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Concrete;
using TahaKonsept.Modules.Providers.Domain.Entities;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence.Repositories.Concrete;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Services;

/// <summary>
/// Aylık harcama (Türkiye saatine göre ay), bütçe kaydı ve bütçe bekçisi (aşınca yeni üretim engeli).
/// </summary>
public sealed class BudgetAndUsageTests : IDisposable
{
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly ProvidersDbContext _db;
    private readonly TestCurrentUser _user = new();
    private readonly CallLogRepository _calls;
    private readonly BudgetSettingRepository _budgets;

    /// <summary>Kurucu.</summary>
    public BudgetAndUsageTests()
    {
        _db = TestFixtures.Db(_clock);
        _calls = new CallLogRepository(_db);
        _budgets = new BudgetSettingRepository(_db);
    }

    /// <summary>Bu ayın kayıtları toplanır; önceki ay ve başka kullanıcı sayılmaz.</summary>
    [Fact]
    public async Task Monthly_usage_sums_this_month_only()
    {
        var ct = TestContext.Current.CancellationToken;
        await AddCallAsync(_user.GetRequiredUserId(), "anthropic", 1.25m, new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero));
        await AddCallAsync(_user.GetRequiredUserId(), "anthropic", 0.75m, new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));
        await AddCallAsync(_user.GetRequiredUserId(), "google", 0.50m, new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero));
        // 31 Ekim 22:00 UTC = 1 Kasım 01:00 Türkiye → Kasım'a dahil
        await AddCallAsync(_user.GetRequiredUserId(), "openai", 0.10m, new DateTimeOffset(2026, 10, 31, 22, 0, 0, TimeSpan.Zero));
        // 31 Ekim 20:00 UTC = 23:00 Türkiye → Ekim
        await AddCallAsync(_user.GetRequiredUserId(), "openai", 9.00m, new DateTimeOffset(2026, 10, 31, 20, 0, 0, TimeSpan.Zero));
        await AddCallAsync(Guid.CreateVersion7(), "anthropic", 5m, new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero));

        var usage = (await new UsageService(_user, _calls, _budgets, _clock).GetMonthlyAsync(ct)).Value;

        usage.Month.ShouldBe("2026-11");
        usage.TotalUsd.ShouldBe(2.60m);
        usage.LimitUsd.ShouldBe(0m);
        usage.Exceeded.ShouldBeFalse();
        usage.ByPlatform[0].Platform.ShouldBe("anthropic");
        usage.ByPlatform[0].CostUsd.ShouldBe(2.00m);
        usage.ByPlatform[0].Calls.ShouldBe(2);
    }

    /// <summary>Limit aşılınca uyarı + yeni üretim engeli; "durdur" kapalıysa engel yok.</summary>
    [Fact]
    public async Task Budget_guard_blocks_new_production_when_exceeded()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = _user.GetRequiredUserId();
        var budgetService = new BudgetService(_user, _budgets, _clock);
        (await budgetService.GetAsync(ct)).Value.MonthlyLimitUsd.ShouldBe(0m);
        await budgetService.SaveAsync(new BudgetSettings { MonthlyLimitUsd = 2m, StopOnExceed = true, WarnAtPercent = 50 }, ct);
        var guard = new BudgetGuard(_calls, _budgets, _clock);

        await AddCallAsync(userId, "anthropic", 1.10m, _clock.UtcNow.AddHours(-1));
        var status = await guard.GetStatusAsync(userId, ct);
        status.Warning.ShouldBeTrue();
        status.Exceeded.ShouldBeFalse();
        (await guard.EnsureCanStartProductionAsync(userId, ct)).IsSuccess.ShouldBeTrue();

        await AddCallAsync(userId, "anthropic", 1.00m, _clock.UtcNow.AddHours(-1));
        var blocked = await guard.EnsureCanStartProductionAsync(userId, ct);
        blocked.IsFailure.ShouldBeTrue();
        blocked.Error.Code.ShouldBe(ProviderErrorCodes.BudgetExceeded);

        var usage = (await new UsageService(_user, _calls, _budgets, _clock).GetMonthlyAsync(ct)).Value;
        usage.Exceeded.ShouldBeTrue();
        usage.LimitUsd.ShouldBe(2m);

        await budgetService.SaveAsync(new BudgetSettings { MonthlyLimitUsd = 2m, StopOnExceed = false, WarnAtPercent = 80 }, ct);
        (await guard.EnsureCanStartProductionAsync(userId, ct)).IsSuccess.ShouldBeTrue();
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    /// <summary>Belirli zamanda çağrı kaydı ekler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="platform">Platform.</param>
    /// <param name="cost">Maliyet.</param>
    /// <param name="at">Zaman.</param>
    private async Task AddCallAsync(Guid userId, string platform, decimal cost, DateTimeOffset at)
    {
        _clock.UtcNow = at;
        await _calls.AddAsync(CallLog.Create(userId, platform, "m", "m", "concept", 1, 1, 0, 0, 0, null, cost, 10, true, null, null), CancellationToken.None);
        await _db.SaveChangesAsync(CancellationToken.None);
        _clock.UtcNow = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    }
}
