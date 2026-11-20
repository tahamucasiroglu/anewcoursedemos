using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Mesai saatleri tablosu (quiet_hours, spec 0011): okul + öğretmen tekil; başlangıç/bitiş <c>time</c> (Türkiye saati),
/// günler bayrak sayısı olarak.
/// </summary>
public sealed class QuietHoursConfiguration : IEntityTypeConfiguration<QuietHours>
{
    public void Configure(EntityTypeBuilder<QuietHours> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("quiet_hours");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedNever();
        builder.Property(q => q.Start).HasColumnType("time without time zone");
        builder.Property(q => q.End).HasColumnType("time without time zone");
        builder.Property(q => q.Days).HasConversion<int>();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(q => q.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(q => q.TeacherUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(q => new { q.TenantId, q.TeacherUserId }).IsUnique();
    }
}
