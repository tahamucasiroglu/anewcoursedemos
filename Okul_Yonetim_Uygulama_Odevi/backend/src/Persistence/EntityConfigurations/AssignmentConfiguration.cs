using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Academics;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Ödev tablosu (assignments, spec 0013): kuruma, şubeye, sınıfa ve veren öğretmene bağlı; teslim günü <c>date</c> (Türkiye
/// günü). Sınıf + teslim günü dizini öğretmen listesini (yeniden eskiye) hızlandırır.
/// </summary>
public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("assignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Subject).HasMaxLength(Assignment.SubjectMax).IsRequired();
        builder.Property(a => a.Title).HasMaxLength(Assignment.TitleMax).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(Assignment.DescriptionMax);
        builder.Property(a => a.DueDate).HasColumnType("date");
        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Classroom>().WithMany().HasForeignKey(a => a.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.ClassroomId, a.DueDate });
    }
}
