using Konsept.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="UserSession"/> tablosunun eşlemesi (<c>identity.sessions</c>): <c>user_id</c> (tüm oturumları iptal) ve
/// tekil <c>refresh_token_hash</c> (dönüş) indeksli. Şemalar arası FK yok (Y-4).
/// </summary>
public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(64).IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.LastUsedAt).HasColumnName("last_used_at").IsRequired();
        builder.Property(s => s.RevokedAt).HasColumnName("revoked_at");
        builder.HasIndex(s => s.UserId).HasDatabaseName("ix_sessions_user_id");
        builder.HasIndex(s => s.RefreshTokenHash).IsUnique().HasDatabaseName("ux_sessions_refresh_token_hash");
    }
}
