using Konsept.Modules.Production.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary><c>production.settings</c>: profil başına tek satır (spec 0006 Req 7); şemalar arası FK yok (mimari kural 7).</summary>
public sealed class ProductionSettingsConfiguration : IEntityTypeConfiguration<ProductionSettings>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "settings";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProductionSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(s => s.ProfileId);
        builder.Property(s => s.ProfileId).HasColumnName("profile_id").ValueGeneratedNever();
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.Subtitles).HasColumnName("subtitles").IsRequired();
        builder.Property(s => s.ProgressBar).HasColumnName("progress_bar").IsRequired();
        builder.Property(s => s.CodeModel).HasColumnName("code_model").HasMaxLength(200);
        builder.Property(s => s.CodeEffort).HasColumnName("code_effort").HasMaxLength(16);
        builder.Property(s => s.Engine).HasColumnName("engine").HasMaxLength(200).IsRequired();
        builder.Property(s => s.ImageModel).HasColumnName("image_model").HasMaxLength(200);
        builder.Property(s => s.AudioModel).HasColumnName("audio_model").HasMaxLength(200);
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasIndex(s => s.UserId).HasDatabaseName("ix_settings_user");
    }
}
