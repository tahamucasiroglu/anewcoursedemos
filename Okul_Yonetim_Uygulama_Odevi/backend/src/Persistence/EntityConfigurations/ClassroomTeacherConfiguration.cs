using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Sınıf öğretmeni tablosu (classroom_teachers, spec 0010): kuruma, şubeye, sınıfa ve kişiye bağlı (silmeyi engelleyen
/// yabancı anahtarlar); aynı kişi aynı sınıfa bir kez atanır (<c>(classroom_id, teacher_user_id)</c> tekil). Öğretmen dizini
/// "Sınıflarım" listesini hızlandırır.
/// </summary>
public sealed class ClassroomTeacherConfiguration : IEntityTypeConfiguration<ClassroomTeacher>
{
    public void Configure(EntityTypeBuilder<ClassroomTeacher> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("classroom_teachers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(t => t.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Classroom>().WithMany().HasForeignKey(t => t.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.TeacherUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new { t.ClassroomId, t.TeacherUserId }).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.TeacherUserId });
    }
}
