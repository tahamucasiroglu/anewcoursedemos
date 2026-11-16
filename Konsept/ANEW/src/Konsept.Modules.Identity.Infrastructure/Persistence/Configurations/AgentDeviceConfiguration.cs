using Konsept.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="AgentDevice"/> tablosunun eşlemesi (<c>identity.agent_devices</c>, spec 0012): <c>user_id</c> (iptal) ve tekil
/// <c>token_hash</c> (hub kimliği) indeksli. Belirtecin kendisi saklanmaz.
/// </summary>
public sealed class AgentDeviceConfiguration : IEntityTypeConfiguration<AgentDevice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgentDevice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("agent_devices");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(d => d.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(d => d.MachineName).HasColumnName("machine_name").HasMaxLength(64).IsRequired();
        builder.Property(d => d.AgentVersion).HasColumnName("agent_version").HasMaxLength(32).IsRequired();
        builder.Property(d => d.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.LastUsedAt).HasColumnName("last_used_at").IsRequired();
        builder.Property(d => d.RevokedAt).HasColumnName("revoked_at");
        builder.HasIndex(d => d.UserId).HasDatabaseName("ix_agent_devices_user_id");
        builder.HasIndex(d => d.TokenHash).IsUnique().HasDatabaseName("ux_agent_devices_token_hash");
    }
}
