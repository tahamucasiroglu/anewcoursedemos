using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>İndirim/burs tanımı tablosu (discount_definitions, spec 0015): kuruma bağlı; yüzde veritabanı <c>CHECK</c> ile 1–100.</summary>
public sealed class DiscountDefinitionConfiguration : IEntityTypeConfiguration<DiscountDefinition>
{
    public void Configure(EntityTypeBuilder<DiscountDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("discount_definitions", t => t.HasCheckConstraint("ck_discount_definitions_percent", "percent >= 1 AND percent <= 100"));
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Name).HasMaxLength(DiscountDefinition.NameMax).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => d.TenantId);
    }
}
