using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="BudgetSetting"/> tablo eşlemesi (<c>providers.budget_settings</c>; kullanıcı başına tek satır).
/// </summary>
public sealed class BudgetSettingConfiguration : IEntityTypeConfiguration<BudgetSetting>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetSetting> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("budget_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.MonthlyLimitUsd).HasPrecision(18, 2);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
