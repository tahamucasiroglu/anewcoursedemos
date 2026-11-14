using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Profiles.Domain.Constants;
using TahaKonsept.Modules.Profiles.Domain.Entities;

namespace TahaKonsept.Modules.Profiles.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="Profile"/> tablo eşlemesi: uzunluk sınırları <see cref="ProfileLimits"/>'ten, biçim metin olarak saklanır,
/// (<c>user_id</c>, <c>is_active</c>) indeksi aktif profil sorgusunu hızlandırır.
/// </summary>
public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("profiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(ProfileLimits.NameMaxLength);
        builder.Property(p => p.Language).IsRequired().HasMaxLength(ProfileLimits.LanguageMaxLength);
        builder.Property(p => p.Format).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.TargetDurationSec).IsRequired();
        builder.Property(p => p.ChannelDescription).IsRequired().HasMaxLength(ProfileLimits.ChannelDescriptionMaxLength);
        builder.Property(p => p.DefaultTone).HasMaxLength(ProfileLimits.StyleFieldMaxLength);
        builder.Property(p => p.DefaultAudience).HasMaxLength(ProfileLimits.StyleFieldMaxLength);
        builder.Property(p => p.DefaultNarrativeStyle).HasMaxLength(ProfileLimits.StyleFieldMaxLength);
        builder.Property(p => p.TemplateKey).HasMaxLength(ProfileLimits.TemplateKeyMaxLength);
        builder.Property(p => p.IsActive).IsRequired();
        builder.HasIndex(p => new { p.UserId, p.IsActive });
    }
}
