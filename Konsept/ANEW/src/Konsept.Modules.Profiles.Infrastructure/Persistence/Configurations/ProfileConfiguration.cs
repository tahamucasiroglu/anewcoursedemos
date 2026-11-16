using Konsept.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Profiles.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="Profile"/> eşlemesi (<c>profiles.profiles</c>): <c>(user_id, name_key)</c> tekildir (tr-TR büyük/küçük harf duyarsız
/// ad, spec 0003 Req 2); dil, biçim ve süre kısıtları veritabanında da vardır.
/// </summary>
public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "profiles";

    /// <summary>Ad tekillik indeksi.</summary>
    public const string NameIndex = "ux_profiles_user_name_key";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName, t =>
        {
            t.HasCheckConstraint("ck_profiles_language", "language IN ('tr', 'en')");
            t.HasCheckConstraint("ck_profiles_format", "format IN ('landscape', 'portrait')");
            t.HasCheckConstraint("ck_profiles_duration", "duration_seconds BETWEEN 15 AND 900");
        });
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        builder.Property(p => p.NameKey).HasColumnName("name_key").HasMaxLength(160).IsRequired();
        builder.Property(p => p.Language).HasColumnName("language").HasMaxLength(8).IsRequired();
        builder.Property(p => p.Format).HasColumnName("format").HasMaxLength(16).IsRequired();
        builder.Property(p => p.DurationSeconds).HasColumnName("duration_seconds").IsRequired();
        builder.Property(p => p.Content).HasColumnName("content").HasMaxLength(2000).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasIndex(p => new { p.UserId, p.NameKey }).IsUnique().HasDatabaseName(NameIndex);
    }
}
