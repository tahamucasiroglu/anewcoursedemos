using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Veli bildirimi tablosu (guardian_notices, spec 0010): kuruma, veliye (kişi) ve öğrenciye bağlı; tür metin olarak.
/// Veli + öğrenci + gün tekil — aynı gün ikinci "yok" bildirimi oluşamaz (C-5, AC-11). Veli + güncelleme dizini listeyi
/// (yeniden eskiye) hızlandırır.
/// </summary>
public sealed class GuardianNoticeConfiguration : IEntityTypeConfiguration<GuardianNotice>
{
    public void Configure(EntityTypeBuilder<GuardianNotice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("guardian_notices");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Day).HasColumnType("date");
        builder.Property(n => n.Kind).HasConversion<string>().HasMaxLength(16);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(n => n.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(n => n.GuardianUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(n => n.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(n => new { n.GuardianUserId, n.StudentId, n.Day }).IsUnique();
        builder.HasIndex(n => new { n.TenantId, n.GuardianUserId, n.UpdatedAt });
    }
}
