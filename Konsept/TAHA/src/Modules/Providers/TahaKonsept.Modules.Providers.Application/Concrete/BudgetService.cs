using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Domain.Entities;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Concrete;

/// <summary>
/// <see cref="IBudgetService"/> uygulaması: oturumdaki kullanıcının bütçe ayarını okur / yazar (kayıt yoksa limitsiz varsayılan).
/// </summary>
public sealed class BudgetService : ProvidersServiceBase, IBudgetService
{
    private readonly ICurrentUser _currentUser;
    private readonly IBudgetSettingRepository _budgets;

    /// <summary>Kurucu.</summary>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="budgets">Bütçe deposu.</param>
    /// <param name="clock">Saat.</param>
    public BudgetService(ICurrentUser currentUser, IBudgetSettingRepository budgets, IClock clock)
        : base(clock)
    {
        _currentUser = currentUser;
        _budgets = budgets;
    }

    /// <inheritdoc />
    public async Task<Result<BudgetSettings>> GetAsync(CancellationToken ct)
    {
        var setting = await _budgets.FindByUserAsync(_currentUser.GetRequiredUserId(), ct);
        return setting is null ? new BudgetSettings() : ToDto(setting);
    }

    /// <inheritdoc />
    public async Task<Result<BudgetSettings>> SaveAsync(BudgetSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var userId = _currentUser.GetRequiredUserId();
        var setting = await _budgets.FindByUserAsync(userId, ct);
        if (setting is null)
        {
            setting = BudgetSetting.Create(userId, settings.MonthlyLimitUsd, settings.StopOnExceed, settings.WarnAtPercent);
            await _budgets.AddAsync(setting, ct);
        }
        else
        {
            setting.Update(settings.MonthlyLimitUsd, settings.StopOnExceed, settings.WarnAtPercent);
        }

        await _budgets.UnitOfWork.SaveChangesAsync(ct);
        return ToDto(setting);
    }

    /// <summary>Varlığı DTO'ya çevirir.</summary>
    /// <param name="setting">Varlık.</param>
    private static BudgetSettings ToDto(BudgetSetting setting) => new()
    {
        MonthlyLimitUsd = setting.MonthlyLimitUsd,
        StopOnExceed = setting.StopOnExceed,
        WarnAtPercent = setting.WarnAtPercent,
    };
}
