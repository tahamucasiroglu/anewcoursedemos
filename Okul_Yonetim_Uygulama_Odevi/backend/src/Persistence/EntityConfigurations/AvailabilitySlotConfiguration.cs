using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Görüşme dilimi tablosu (availability_slots, spec 0016): kuruma ve öğretmene (kişi) bağlı; başlangıç/bitiş <c>timestamptz</c> (UTC).
/// <c>CHECK</c>: bitiş başlangıçtan sonra. Öğretmen + başlangıç dizini takvimi ve çakışma denetimini hızlandırır.
/// </summary>
public sealed class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("availability_slots", t => t.HasCheckConstraint("ck_availability_slots_range", "ends_at > starts_at"));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.TeacherUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.TeacherUserId, s.StartsAt });
        builder.HasIndex(s => s.TenantId);
    }
}
