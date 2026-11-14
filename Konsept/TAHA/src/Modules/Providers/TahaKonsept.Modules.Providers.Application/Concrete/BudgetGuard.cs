using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.Concrete;

/// <summary>
/// <see cref="IBudgetGuard"/> uygulaması: bu ayın (Türkiye saati) CallLog toplamını bütçe ayarıyla karşılaştırır.
/// </summary>
public sealed class BudgetGuard : ProvidersServiceBase, IBudgetGuard
{
    private readonly ICallLogRepository _calls;
    private readonly IBudgetSettingRepository _budgets;

    /// <summary>Kurucu.</summary>
    /// <param name="calls">Çağrı günlüğü deposu.</param>
    /// <param name="budgets">Bütçe deposu.</param>
    /// <param name="clock">Saat.</param>
    public BudgetGuard(ICallLogRepository calls, IBudgetSettingRepository budgets, IClock clock)
        : base(clock)
    {
        _calls = calls;
        _budgets = budgets;
    }

    /// <inheritdoc />
    public async Task<BudgetStatus> GetStatusAsync(Guid userId, CancellationToken ct)
    {
        var (label, start, end) = BillingMonth.For(Clock.UtcNow);
        var byPlatform = await _calls.SumByPlatformAsync(userId, start, end, ct);
        var setting = await _budgets.FindByUserAsync(userId, ct);
        return new BudgetStatus
        {
            Month = label,
            SpentUsd = decimal.Round(byPlatform.Sum(p => p.CostUsd), 4),
            LimitUsd = setting?.MonthlyLimitUsd ?? 0m,
            StopOnExceed = setting?.StopOnExceed ?? true,
            WarnAtPercent = setting?.WarnAtPercent ?? 80,
        };
    }

    /// <inheritdoc />
    public async Task<Result> EnsureCanStartProductionAsync(Guid userId, CancellationToken ct)
    {
        var status = await GetStatusAsync(userId, ct);
        return status.BlocksNewProduction
            ? Error.Conflict(
                ProviderErrorCodes.BudgetExceeded,
                $"Aylık bütçe limiti aşıldı (${status.SpentUsd:0.00} / ${status.LimitUsd:0.00}). Yeni üretim başlatılamaz; Ayarlar → Bütçe limiti.")
            : Result.Success();
    }
}
