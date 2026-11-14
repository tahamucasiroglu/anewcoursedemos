using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Concrete;

/// <summary>
/// <see cref="IUsageService"/> uygulaması: oturumdaki kullanıcının bu ayki harcaması (toplam + platform kırılımı + limit).
/// </summary>
public sealed class UsageService : ProvidersServiceBase, IUsageService
{
    private readonly ICurrentUser _currentUser;
    private readonly ICallLogRepository _calls;
    private readonly IBudgetSettingRepository _budgets;

    /// <summary>Kurucu.</summary>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="calls">Çağrı günlüğü deposu.</param>
    /// <param name="budgets">Bütçe deposu.</param>
    /// <param name="clock">Saat.</param>
    public UsageService(ICurrentUser currentUser, ICallLogRepository calls, IBudgetSettingRepository budgets, IClock clock)
        : base(clock)
    {
        _currentUser = currentUser;
        _calls = calls;
        _budgets = budgets;
    }

    /// <inheritdoc />
    public async Task<Result<MonthlyUsageResponse>> GetMonthlyAsync(CancellationToken ct)
    {
        var userId = _currentUser.GetRequiredUserId();
        var (label, start, end) = BillingMonth.For(Clock.UtcNow);
        var byPlatform = await _calls.SumByPlatformAsync(userId, start, end, ct);
        var setting = await _budgets.FindByUserAsync(userId, ct);
        var total = decimal.Round(byPlatform.Sum(p => p.CostUsd), 4);
        var limit = setting?.MonthlyLimitUsd ?? 0m;
        var warnAt = setting?.WarnAtPercent ?? 80;
        return new MonthlyUsageResponse
        {
            Month = label,
            TotalUsd = total,
            LimitUsd = limit,
            Exceeded = limit > 0 && total >= limit,
            Warning = limit > 0 && total >= limit * warnAt / 100m,
            StopOnExceed = setting?.StopOnExceed ?? true,
            ByPlatform = byPlatform.OrderByDescending(p => p.CostUsd).ToList(),
        };
    }
}
