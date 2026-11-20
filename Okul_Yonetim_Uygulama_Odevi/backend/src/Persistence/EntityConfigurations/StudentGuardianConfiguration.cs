using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Veli-öğrenci bağı tablosu (student_guardians): kuruma, öğrenciye ve kişiye bağlı; aynı kişi aynı öğrenciye bir kez
/// bağlanır (<c>(student_id, guardian_user_id)</c> tekil). Yakınlık metin olarak tutulur.
/// </summary>
public sealed class StudentGuardianConfiguration : IEntityTypeConfiguration<StudentGuardian>
{
    public void Configure(EntityTypeBuilder<StudentGuardian> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("student_guardians");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Relationship).HasConversion<string>().HasMaxLength(16);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(g => g.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(g => g.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(g => g.GuardianUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(g => new { g.StudentId, g.GuardianUserId }).IsUnique();
        builder.HasIndex(g => new { g.TenantId, g.GuardianUserId });
    }
}
