using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Öğrenci tablosu (students): kuruma, şubeye ve (isteğe bağlı) sınıfa bağlı — silmeyi engelleyen yabancı anahtarlar.
/// Okul numarası kurum başına tekil (küçük harf biçimi <c>school_number_key</c> üzerinde, boş olanlar hariç). Sağlık
/// alanları yalnızca şifreli metin (<c>…_cipher</c>). <c>IsActive</c> için model varsayılanı yok (T14).
/// </summary>
public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("students");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.FullName).HasMaxLength(Student.FullNameMaxLength).IsRequired();
        builder.Property(s => s.BirthDate).HasColumnType("date");
        builder.Property(s => s.SchoolNumber).HasMaxLength(Student.SchoolNumberMaxLength);
        builder.Property(s => s.SchoolNumberKey).HasMaxLength(Student.SchoolNumberMaxLength);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Classroom>().WithMany().HasForeignKey(s => s.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.BranchId);
        builder.HasIndex(s => new { s.TenantId, s.SchoolNumberKey }).IsUnique().HasFilter("school_number_key IS NOT NULL");
    }
}
