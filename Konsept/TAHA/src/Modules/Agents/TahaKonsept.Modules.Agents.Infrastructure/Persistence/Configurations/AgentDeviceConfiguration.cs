using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Agents.Domain.Entities;

namespace TahaKonsept.Modules.Agents.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="AgentDevice"/> tablo eşlemesi (<c>agents.agent_devices</c>): alan uzunlukları, yetenekler <c>jsonb</c>,
/// kullanıcı + iptal ve token ailesi indeksleri.
/// </summary>
public sealed class AgentDeviceConfiguration : IEntityTypeConfiguration<AgentDevice>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Oluşturucu.</param>
    public void Configure(EntityTypeBuilder<AgentDevice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("agent_devices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.MachineName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AgentVersion).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OsDescription).HasMaxLength(256);
        builder.Property(x => x.LanAddress).HasMaxLength(64);
        builder.Property(x => x.StreamKeyProtected).HasMaxLength(1024);
        builder.Property(x => x.CapabilitiesJson).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.UserId, x.IsRevoked });
        builder.HasIndex(x => x.TokenFamilyId);
    }
}
