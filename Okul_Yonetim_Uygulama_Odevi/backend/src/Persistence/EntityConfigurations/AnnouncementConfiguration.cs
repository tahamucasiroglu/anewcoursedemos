using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Duyuru tablosu (announcements, spec 0011): kuruma ve yayınlayana bağlı; kapsam metin olarak, kapsam kimliği (şube/sınıf/
/// öğrenci) yabancı anahtarsız (türüne göre farklı tablo). Başlık 150, metin 4000, onay sorusu 300 karakter; son gün
/// <c>date</c>. Kurum + yayın zamanı dizini kurucunun listesini, yayınlayan dizini öğretmenin listesini hızlandırır.
/// </summary>
public sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("announcements");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Scope).HasConversion<string>().HasMaxLength(16);
        builder.Property(a => a.Title).HasMaxLength(Announcement.TitleMaxLength);
        builder.Property(a => a.Body).HasMaxLength(Announcement.BodyMaxLength);
        builder.Property(a => a.ConsentQuestion).HasMaxLength(Announcement.QuestionMaxLength);
        builder.Property(a => a.ConsentDeadline).HasColumnType("date");
        builder.Ignore(a => a.RequiresConsent);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.TenantId, a.PublishedAt });
        builder.HasIndex(a => new { a.TenantId, a.AuthorUserId, a.PublishedAt });
    }
}
