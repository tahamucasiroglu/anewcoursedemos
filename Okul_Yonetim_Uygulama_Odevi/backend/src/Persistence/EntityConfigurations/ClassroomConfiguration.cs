using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Sınıf tablosu (classrooms): kuruma, döneme, şubeye ve seviyeye bağlı (silmeyi engelleyen yabancı anahtarlar), ad
/// zorunlu. Dönem + şube dizini listeyi hızlandırır. <c>IsActive</c> için model varsayılanı yok (T14).
/// </summary>
public sealed class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("classrooms");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(30).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Term>().WithMany().HasForeignKey(c => c.TermId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GradeLevel>().WithMany().HasForeignKey(c => c.GradeLevelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TermId, c.BranchId });
    }
}
