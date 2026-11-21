using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Academics;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Değerlendirme tablosu (assessments, spec 0013): kuruma, şubeye, sınıfa ve açan öğretmene bağlı; tür metin, gün <c>date</c>.
/// Sınıf + gün dizini öğretmen listesini hızlandırır.
/// </summary>
public sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("assessments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Subject).HasMaxLength(Assessment.SubjectMax).IsRequired();
        builder.Property(a => a.Title).HasMaxLength(Assessment.TitleMax).IsRequired();
        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(a => a.Date).HasColumnType("date");
        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Classroom>().WithMany().HasForeignKey(a => a.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.ClassroomId, a.Date });
    }
}
