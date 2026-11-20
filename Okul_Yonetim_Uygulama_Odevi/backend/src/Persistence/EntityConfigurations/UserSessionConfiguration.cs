using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>Oturum tablosu (user_sessions): yenileme anahtarının yalnızca özeti, tekil indeksli.</summary>
public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("user_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.RefreshTokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(s => s.RefreshTokenHash).IsUnique();
        builder.Property(s => s.PreviousRefreshTokenHash).HasMaxLength(128);
        builder.HasIndex(s => s.PreviousRefreshTokenHash);
        builder.Property(s => s.CreatedIp).HasMaxLength(64);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
