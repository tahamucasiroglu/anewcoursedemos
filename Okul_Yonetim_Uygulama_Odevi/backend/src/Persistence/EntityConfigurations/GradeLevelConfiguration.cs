using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Sınıf seviyesi tablosu (grade_levels): kuruma bağlı (yabancı anahtar), ad zorunlu. Ad tekilliği veritabanında değil,
/// uygulamada Türkçe harf kuralıyla ve okul başına sırayla denetlenir (pasif kayıt aynı adı taşıyabilir). <c>IsActive</c>
/// için model varsayılanı verilmez (T14); veritabanı varsayılanı yalnızca migration'da.
/// </summary>
public sealed class GradeLevelConfiguration : IEntityTypeConfiguration<GradeLevel>
{
    public void Configure(EntityTypeBuilder<GradeLevel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("grade_levels");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Name).HasMaxLength(50).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(g => g.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(g => g.TenantId);
    }
}
