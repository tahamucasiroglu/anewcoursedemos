using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="IdentitySetting"/> eşlemesi: tablo <c>settings</c>, birincil anahtar <c>key</c> (64), değer en fazla 256 karakter.
/// </summary>
public sealed class IdentitySettingConfiguration : IEntityTypeConfiguration<IdentitySetting>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<IdentitySetting> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("settings");
        builder.HasKey(s => s.Key);
        builder.Property(s => s.Key).HasMaxLength(64).ValueGeneratedNever();
        builder.Property(s => s.Value).HasMaxLength(256).IsRequired();
        builder.Property(s => s.UpdatedAtUtc);
    }
}
