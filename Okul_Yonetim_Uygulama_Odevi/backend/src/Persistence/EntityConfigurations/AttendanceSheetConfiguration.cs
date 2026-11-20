using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Attendance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Yoklama tablosu (attendance_sheets, spec 0010): kuruma, şubeye, sınıfa ve son kaydeden kişiye bağlı; gün <c>date</c>
/// (Türkiye günü). Sınıf + gün tekil — eşzamanlı iki kayıtta bile tek yoklama kalır (AC-11; asıl sıra uygulamadaki kilitte).
/// </summary>
public sealed class AttendanceSheetConfiguration : IEntityTypeConfiguration<AttendanceSheet>
{
    public void Configure(EntityTypeBuilder<AttendanceSheet> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("attendance_sheets");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Day).HasColumnType("date");
        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Classroom>().WithMany().HasForeignKey(s => s.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.RecordedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.ClassroomId, s.Day }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.Day });
    }
}
