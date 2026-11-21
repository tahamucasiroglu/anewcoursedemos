using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>Makbuz sayacı tablosu (receipt_counters, spec 0015): okul başına tek satır (anahtar = okul), son numara 0 ya da büyük.</summary>
public sealed class ReceiptCounterConfiguration : IEntityTypeConfiguration<ReceiptCounter>
{
    public void Configure(EntityTypeBuilder<ReceiptCounter> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("receipt_counters", t => t.HasCheckConstraint("ck_receipt_counters_last", "last_number >= 0"));
        builder.HasKey(c => c.TenantId);
        builder.Property(c => c.TenantId).ValueGeneratedNever();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
