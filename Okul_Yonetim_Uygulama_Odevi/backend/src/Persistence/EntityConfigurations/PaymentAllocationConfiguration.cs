using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>Tahsilat dağılımı tablosu (payment_allocations, spec 0015): tahsilat + taksit tekil, tutar <c>numeric(18,2)</c> (<c>CHECK</c> sıfırdan büyük).</summary>
public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("payment_allocations", t => t.HasCheckConstraint("ck_payment_allocations_amount", "amount > 0"));
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Amount).HasPrecision(18, 2);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Installment>().WithMany().HasForeignKey(a => a.InstallmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.PaymentId, a.InstallmentId }).IsUnique();
        builder.HasIndex(a => a.InstallmentId);
    }
}
