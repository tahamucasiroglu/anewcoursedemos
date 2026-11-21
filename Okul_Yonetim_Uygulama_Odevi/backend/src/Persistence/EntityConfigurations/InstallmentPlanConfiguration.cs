using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Taksit planı tablosu (installment_plans, spec 0015): kuruma, şubeye (öğrencinin şubesi), öğrenciye ve tarifeye bağlı; açılıştaki
/// tarife/indirim bilgisi ve net <c>numeric(18,2)</c>. <c>CHECK</c>: net sıfırdan büyük ve tarife − indirim, taksit sayısı 1–12.
/// Öğrencinin aynı tarifeden geçerli ikinci planı filtreli tekil dizinle de engellenir (uygulama kuralının ikinci katmanı).
/// </summary>
public sealed class InstallmentPlanConfiguration : IEntityTypeConfiguration<InstallmentPlan>
{
    public void Configure(EntityTypeBuilder<InstallmentPlan> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("installment_plans", t =>
        {
            t.HasCheckConstraint("ck_installment_plans_net", "net_amount > 0 AND discount_amount >= 0 AND net_amount = fee_amount - discount_amount");
            t.HasCheckConstraint("ck_installment_plans_count", "installment_count >= 1 AND installment_count <= 12");
        });
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.FeeName).HasMaxLength(FeeSchedule.NameMax).IsRequired();
        builder.Property(p => p.DiscountName).HasMaxLength(DiscountDefinition.NameMax);
        builder.Property(p => p.FeeAmount).HasPrecision(18, 2);
        builder.Property(p => p.DiscountAmount).HasPrecision(18, 2);
        builder.Property(p => p.NetAmount).HasPrecision(18, 2);
        builder.Property(p => p.FirstDueDate).HasColumnType("date");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(p => p.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FeeSchedule>().WithMany().HasForeignKey(p => p.FeeScheduleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DiscountDefinition>().WithMany().HasForeignKey(p => p.DiscountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.StudentId, p.FeeScheduleId }).IsUnique().HasFilter("status = 'Active'");
        builder.HasIndex(p => new { p.TenantId, p.BranchId });
    }
}
