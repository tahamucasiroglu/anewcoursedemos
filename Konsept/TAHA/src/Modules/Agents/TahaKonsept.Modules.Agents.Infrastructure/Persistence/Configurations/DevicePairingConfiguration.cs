using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Agents.Domain.Entities;

namespace TahaKonsept.Modules.Agents.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="DevicePairing"/> tablo eşlemesi (<c>agents.device_pairings</c>): cihaz kodu hash'i benzersiz, kullanıcı kodu indeksli,
/// durum metin olarak.
/// </summary>
public sealed class DevicePairingConfiguration : IEntityTypeConfiguration<DevicePairing>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Oluşturucu.</param>
    public void Configure(EntityTypeBuilder<DevicePairing> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("device_pairings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DeviceCodeHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UserCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.MachineName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AgentVersion).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(x => x.DeviceCodeHash).IsUnique();
        builder.HasIndex(x => x.UserCode);
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
