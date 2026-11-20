using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Attendance;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Yoklama satırı tablosu (attendance_entries, spec 0010): yoklamaya ve öğrenciye bağlı, durum metin olarak; yoklama +
/// öğrenci tekil.
/// </summary>
public sealed class AttendanceEntryConfiguration : IEntityTypeConfiguration<AttendanceEntry>
{
    public void Configure(EntityTypeBuilder<AttendanceEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("attendance_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AttendanceSheet>().WithMany().HasForeignKey(e => e.SheetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.SheetId, e.StudentId }).IsUnique();
        builder.HasIndex(e => e.StudentId);
    }
}
