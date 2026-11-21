using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Ücret tarifesi tablosu (fee_schedules, spec 0015): kuruma bağlı; tutar <c>numeric(18,2)</c>, veritabanı <c>CHECK</c> ile 0'dan
/// büyük ve en fazla 10.000.000,00 (doğrulamanın ikinci katmanı). Etkinler arasında ad tekilliği Türkçe harf kuralıyla uygulamada,
/// okul başına sırada (0005 şube adı kalıbı).
/// </summary>
public sealed class FeeScheduleConfiguration : IEntityTypeConfiguration<FeeSchedule>
{
    public void Configure(EntityTypeBuilder<FeeSchedule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("fee_schedules", t => t.HasCheckConstraint("ck_fee_schedules_amount", "amount > 0 AND amount <= 10000000.00"));
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Name).HasMaxLength(FeeSchedule.NameMax).IsRequired();
        builder.Property(f => f.Amount).HasPrecision(18, 2);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(f => f.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(f => f.TenantId);
    }
}
