using Konsept.Modules.Production.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary><c>production.clip_tombstones</c>: kalıcı silinen videonun klip izi (spec 0019, B-130); içerik yok.</summary>
public sealed class ClipTombstoneConfiguration : IEntityTypeConfiguration<ClipTombstone>
{
    /// <summary>Tablo adı.</summary>
    public const string TableName = "clip_tombstones";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ClipTombstone> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(TableName);
        builder.HasKey(t => t.ConceptId);
        builder.Property(t => t.ConceptId).HasColumnName("concept_id").ValueGeneratedNever();
        builder.Property(t => t.FirstCompletedAt).HasColumnName("first_completed_at").IsRequired();
    }
}
