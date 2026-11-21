using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Taksit tablosu (installments, spec 0015): plana bağlı, plan + sıra tekil; vade <c>date</c>, tutar ve ödenen <c>numeric(18,2)</c>.
/// <c>CHECK</c>: tutar sıfırdan büyük, ödenen 0 ile tutar arasında — hatalı bir yazma taksidi fazla ödenmiş bırakamaz.
/// </summary>
public sealed class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("installments", t => t.HasCheckConstraint("ck_installments_amounts", "amount > 0 AND paid_amount >= 0 AND paid_amount <= amount"));
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.DueDate).HasColumnType("date");
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);
        builder.Ignore(i => i.Remaining);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InstallmentPlan>().WithMany().HasForeignKey(i => i.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => new { i.PlanId, i.Sequence }).IsUnique();
        builder.HasIndex(i => new { i.TenantId, i.DueDate });
    }
}
