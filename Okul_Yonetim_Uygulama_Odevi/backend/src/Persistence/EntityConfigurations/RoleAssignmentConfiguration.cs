using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Rol ataması tablosu (role_assignments): kişi, kurum ve (varsa) şube yabancı anahtarlı; rol metin olarak saklanır.
/// Aynı kişiye aynı kurumda aynı rol aynı şubede bir kez verilir (kurucu için şube boş — tekil indeks NULL'ları eşit sayar).
/// </summary>
public sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("role_assignments");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Role).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.UserId, r.TenantId, r.Role, r.BranchId }).IsUnique().AreNullsDistinct(false);
        builder.HasIndex(r => r.TenantId);
    }
}
