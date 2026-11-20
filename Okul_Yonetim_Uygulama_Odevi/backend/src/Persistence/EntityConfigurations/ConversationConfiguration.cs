using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Konuşma tablosu (conversations, spec 0011): okul + öğretmen + veli + öğrenci tekil (aynı üçlü için tek konuşma). Öğretmen
/// ve veli dizinleri konuşma listesini (son mesaja göre) hızlandırır.
/// </summary>
public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("conversations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.TeacherUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.GuardianUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(c => c.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => new { c.TenantId, c.TeacherUserId, c.GuardianUserId, c.StudentId }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.GuardianUserId, c.LastMessageAt });
        builder.HasIndex(c => new { c.TenantId, c.TeacherUserId, c.LastMessageAt });
    }
}
