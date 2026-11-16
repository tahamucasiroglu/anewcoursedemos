using Konsept.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Profiles.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ConceptSettings"/> eşlemesi (<c>profiles.concept_settings</c>, spec 0005): profil başına tek satır; profil silinince
/// satır da silinir (aynı şema içinde FK cascade).
/// </summary>
public sealed class ConceptSettingsConfiguration : IEntityTypeConfiguration<ConceptSettings>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "concept_settings";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConceptSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(s => s.ProfileId);
        builder.Property(s => s.ProfileId).HasColumnName("profile_id").ValueGeneratedNever();
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.ConceptModel).HasColumnName("concept_model").HasMaxLength(200);
        builder.Property(s => s.ConceptEffort).HasColumnName("concept_effort").HasMaxLength(32);
        builder.Property(s => s.DedupeModel).HasColumnName("dedupe_model").HasMaxLength(200);
        builder.Property(s => s.DedupeEffort).HasColumnName("dedupe_effort").HasMaxLength(32);
        builder.Property(s => s.TopicModel).HasColumnName("topic_model").HasMaxLength(200);
        builder.Property(s => s.TopicEffort).HasColumnName("topic_effort").HasMaxLength(32);
        builder.Property(s => s.Tone).HasColumnName("tone").HasMaxLength(32);
        builder.Property(s => s.Audience).HasColumnName("audience").HasMaxLength(32);
        builder.Property(s => s.Narration).HasColumnName("narration").HasMaxLength(32);
        builder.Property(s => s.WordCheck).HasColumnName("word_check").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasIndex(s => s.UserId).HasDatabaseName("ix_concept_settings_user");
        builder.HasOne<Profile>().WithOne().HasForeignKey<ConceptSettings>(s => s.ProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}
