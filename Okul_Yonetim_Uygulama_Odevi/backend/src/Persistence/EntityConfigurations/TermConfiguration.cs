using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>Dönem tablosu (terms): kuruma bağlı, ad zorunlu, başlangıç ve bitiş <c>date</c> (saatsiz Türkiye günü).</summary>
public sealed class TermConfiguration : IEntityTypeConfiguration<Term>
{
    public void Configure(EntityTypeBuilder<Term> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("terms");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.StartDate).HasColumnType("date");
        builder.Property(t => t.EndDate).HasColumnType("date");
        builder.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.TenantId);
    }
}
