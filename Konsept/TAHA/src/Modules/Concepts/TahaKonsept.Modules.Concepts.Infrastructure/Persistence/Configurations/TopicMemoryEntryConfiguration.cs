using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.Entities;

namespace TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="TopicMemoryEntry"/> tablo eşlemesi (<c>topic_memory</c>): tür metin; (<c>user_id</c>, <c>profile_id</c>) ve <c>concept_id</c> indeksleri.
/// </summary>
public sealed class TopicMemoryEntryConfiguration : IEntityTypeConfiguration<TopicMemoryEntry>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<TopicMemoryEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("topic_memory");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(ConceptLimits.TitleMaxLength);
        builder.Property(e => e.Keywords).IsRequired().HasMaxLength(ConceptLimits.KeywordsMaxLength);
        builder.Property(e => e.Hash).IsRequired().HasMaxLength(32);
        builder.Property(e => e.Kind).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Ignore(e => e.IsNegative);
        builder.HasIndex(e => new { e.UserId, e.ProfileId });
        builder.HasIndex(e => e.ConceptId);
    }
}
