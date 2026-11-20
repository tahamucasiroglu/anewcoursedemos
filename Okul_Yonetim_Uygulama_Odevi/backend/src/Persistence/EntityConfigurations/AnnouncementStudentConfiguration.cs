using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>Duyurunun kapsadığı öğrenci tablosu (announcement_students, spec 0011): duyuru + öğrenci tekil.</summary>
public sealed class AnnouncementStudentConfiguration : IEntityTypeConfiguration<AnnouncementStudent>
{
    public void Configure(EntityTypeBuilder<AnnouncementStudent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("announcement_students");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Announcement>().WithMany().HasForeignKey(s => s.AnnouncementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.AnnouncementId, s.StudentId }).IsUnique();
    }
}
