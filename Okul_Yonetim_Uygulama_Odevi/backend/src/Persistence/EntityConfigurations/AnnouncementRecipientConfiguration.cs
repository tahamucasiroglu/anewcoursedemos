using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Duyuru alıcısı tablosu (announcement_recipients, spec 0011): duyuru + veli tekil (veli başına tek satır, tek bildirim).
/// Veli dizini velinin akışını ve okunmamış sayısını hızlandırır.
/// </summary>
public sealed class AnnouncementRecipientConfiguration : IEntityTypeConfiguration<AnnouncementRecipient>
{
    public void Configure(EntityTypeBuilder<AnnouncementRecipient> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("announcement_recipients");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Announcement>().WithMany().HasForeignKey(r => r.AnnouncementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.GuardianUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.AnnouncementId, r.GuardianUserId }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.GuardianUserId });
    }
}
