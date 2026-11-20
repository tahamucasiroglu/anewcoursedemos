using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Onay cevabı tablosu (consent_responses, spec 0011): duyuru + öğrenci tekil — öğrenci başına tek geçerli cevap (son cevap);
/// cevap metin olarak, cevaplayan veliye bağlı.
/// </summary>
public sealed class ConsentResponseConfiguration : IEntityTypeConfiguration<ConsentResponse>
{
    public void Configure(EntityTypeBuilder<ConsentResponse> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("consent_responses");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Answer).HasConversion<string>().HasMaxLength(16);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Announcement>().WithMany().HasForeignKey(c => c.AnnouncementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(c => c.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.AnsweredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => new { c.AnnouncementId, c.StudentId }).IsUnique();
    }
}
