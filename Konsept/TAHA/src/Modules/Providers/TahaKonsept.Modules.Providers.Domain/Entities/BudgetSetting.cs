using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;

namespace TahaKonsept.Modules.Providers.Domain.Entities;

/// <summary>
/// Kullanıcının aylık bütçe ayarı (kullanıcı başına tek kayıt). <see cref="MonthlyLimitUsd"/> 0 ise limitsiz.
/// Önemli üyeler: <see cref="Create"/>, <see cref="Update"/>.
/// </summary>
public sealed class BudgetSetting : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için.</summary>
    private BudgetSetting()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Aylık limit ($; 0 = limitsiz).</summary>
    public decimal MonthlyLimitUsd { get; private set; }

    /// <summary>Aşınca yeni üretimi durdur.</summary>
    public bool StopOnExceed { get; private set; } = true;

    /// <summary>Uyarı eşiği (limitin yüzdesi).</summary>
    public int WarnAtPercent { get; private set; } = 80;

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni ayar oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="monthlyLimitUsd">Limit.</param>
    /// <param name="stopOnExceed">Aşınca durdur.</param>
    /// <param name="warnAtPercent">Uyarı eşiği.</param>
    public static BudgetSetting Create(Guid userId, decimal monthlyLimitUsd, bool stopOnExceed, int warnAtPercent)
    {
        var entity = new BudgetSetting { UserId = userId };
        entity.Update(monthlyLimitUsd, stopOnExceed, warnAtPercent);
        return entity;
    }

    /// <summary>Ayarı günceller (değerler sınırlara çekilir).</summary>
    /// <param name="monthlyLimitUsd">Limit.</param>
    /// <param name="stopOnExceed">Aşınca durdur.</param>
    /// <param name="warnAtPercent">Uyarı eşiği (1-100).</param>
    public void Update(decimal monthlyLimitUsd, bool stopOnExceed, int warnAtPercent)
    {
        MonthlyLimitUsd = Math.Max(0, decimal.Round(monthlyLimitUsd, 2));
        StopOnExceed = stopOnExceed;
        WarnAtPercent = Math.Clamp(warnAtPercent, 1, 100);
    }
}
