using System.Text.Json;
using Konsept.Modules.Concepts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Concepts.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>concepts.concepts</c>: konsept başına satır; sahneler sıralı <c>jsonb</c> dizisi; durum tam sayı; profil değerleri satırın
/// kendisinde (Req 27). Profil başına liste sorgusu için (user_id, profile_id, created_at) dizini.
/// </summary>
public sealed class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "concepts";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Concept> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(c => c.TitleKey).HasColumnName("title_key").HasMaxLength(500).IsRequired();
        builder.Property(c => c.Source).HasColumnName("source").IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").IsRequired();
        builder.Property(c => c.Language).HasColumnName("language").HasMaxLength(8).IsRequired();
        builder.Property(c => c.Format).HasColumnName("format").HasMaxLength(16).IsRequired();
        builder.Property(c => c.TargetDurationSeconds).HasColumnName("target_duration_seconds").IsRequired();
        builder.Property(c => c.Scenes)
            .HasColumnName("scenes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<List<ConceptScene>>(v, Json) ?? new List<ConceptScene>(),
                new ValueComparer<IReadOnlyList<ConceptScene>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                    v => v.ToList()))
            .IsRequired();
        builder.Property(c => c.WordRepetitionWarning).HasColumnName("word_repetition_warning").IsRequired();
        builder.Property(c => c.DurationMismatchWarning).HasColumnName("duration_mismatch_warning").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.HasIndex(c => new { c.UserId, c.ProfileId, c.CreatedAt }).HasDatabaseName("ix_concepts_user_profile_created");
    }
}
