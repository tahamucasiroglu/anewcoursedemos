using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Randevu tablosu (appointments, spec 0016): kuruma, dilime, öğretmene, veliye (kişi) ve öğrenciye bağlı; dilimin başlangıç/bitişi
/// kopyalanır (<c>timestamptz</c>), durum sayı olarak (1–5). <c>CHECK</c>: durum 1–5, bitiş başlangıçtan sonra. <b>Kısmi tekil dizin</b>
/// <c>appointments(slot_id) WHERE status IN (1, 2)</c>: bir dilimde tek açık randevu — öğretmen sırasına ek ikinci savunma
/// (PostgreSQL 17'de de var, T12). Veli ve çocuk + öğretmen dizinleri listeleri ve açık randevu denetimini hızlandırır.
/// </summary>
public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("appointments", t =>
        {
            t.HasCheckConstraint("ck_appointments_status", "status BETWEEN 1 AND 5");
            t.HasCheckConstraint("ck_appointments_range", "ends_at > starts_at");
        });
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Topic).HasMaxLength(Appointment.TopicMax).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(Appointment.ReasonMax);
        builder.Property(a => a.Status).HasConversion<int>();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AvailabilitySlot>().WithMany().HasForeignKey(a => a.SlotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.TeacherUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.GuardianUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.SlotId).IsUnique().HasFilter("status IN (1, 2)");
        builder.HasIndex(a => a.GuardianUserId);
        builder.HasIndex(a => new { a.StudentId, a.TeacherUserId });
        builder.HasIndex(a => new { a.TeacherUserId, a.StartsAt });
    }
}
