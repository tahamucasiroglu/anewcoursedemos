using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Academics;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Puan tablosu (grades, spec 0013): değerlendirmeye ve öğrenciye bağlı; puan boş ya da 0–100 (veritabanı <c>CHECK</c> — iki
/// katmanlı doğrulamanın ikinci katmanı, plan Risks 6). Değerlendirme + öğrenci tekil; öğrenci dizini öğrenci bazlı okuma için.
/// </summary>
public sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("grades", t => t.HasCheckConstraint("ck_grades_score_range", "score IS NULL OR (score >= 0 AND score <= 100)"));
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(g => g.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Assessment>().WithMany().HasForeignKey(g => g.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(g => g.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(g => g.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(g => new { g.AssessmentId, g.StudentId }).IsUnique();
        builder.HasIndex(g => g.StudentId);
    }
}
