using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>Teslim alabilecek kişi tablosu (pickup_persons): kuruma ve öğrenciye bağlı; ad, yakınlık ve telefon zorunlu.</summary>
public sealed class PickupPersonConfiguration : IEntityTypeConfiguration<PickupPerson>
{
    public void Configure(EntityTypeBuilder<PickupPerson> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("pickup_persons");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.FullName).HasMaxLength(PickupPerson.FullNameMaxLength).IsRequired();
        builder.Property(p => p.Relationship).HasMaxLength(PickupPerson.RelationshipMaxLength).IsRequired();
        builder.Property(p => p.Phone).HasMaxLength(20).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(p => p.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.StudentId);
    }
}
