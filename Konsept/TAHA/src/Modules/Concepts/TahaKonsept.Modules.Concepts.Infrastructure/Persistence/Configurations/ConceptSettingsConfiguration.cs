using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.Entities;

namespace TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ConceptSettings"/> tablo eşlemesi (<c>settings</c>): kullanıcı başına tek kayıt (benzersiz <c>user_id</c>), efor metin.
/// </summary>
public sealed class ConceptSettingsConfiguration : IEntityTypeConfiguration<ConceptSettings>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<ConceptSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.ConceptModelKey).IsRequired().HasMaxLength(ConceptLimits.ModelKeyMaxLength);
        builder.Property(s => s.TopicModelKey).IsRequired().HasMaxLength(ConceptLimits.ModelKeyMaxLength);
        builder.Property(s => s.WordCheckModelKey).IsRequired().HasMaxLength(ConceptLimits.ModelKeyMaxLength);
        builder.Property(s => s.ConceptEffort).HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.Tone).HasMaxLength(ConceptLimits.StyleMaxLength);
        builder.Property(s => s.Audience).HasMaxLength(ConceptLimits.StyleMaxLength);
        builder.Property(s => s.NarrativeStyle).HasMaxLength(ConceptLimits.StyleMaxLength);
        builder.Property(s => s.Notes).HasMaxLength(ConceptLimits.NotesMaxLength);
        builder.HasIndex(s => s.UserId).IsUnique();
    }
}
