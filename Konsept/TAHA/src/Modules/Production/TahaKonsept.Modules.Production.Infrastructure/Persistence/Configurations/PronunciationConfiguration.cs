using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="Pronunciation"/> tablo eşlemesi (<c>production.pronunciations</c>): (<c>user_id</c>, <c>written_key</c>) benzersiz; durum metin olarak.
/// </summary>
public sealed class PronunciationConfiguration : IEntityTypeConfiguration<Pronunciation>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<Pronunciation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("pronunciations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Written).IsRequired().HasMaxLength(ProductionLimits.PronunciationMaxLength);
        builder.Property(x => x.WrittenKey).IsRequired().HasMaxLength(ProductionLimits.PronunciationMaxLength);
        builder.Property(x => x.Spoken).IsRequired().HasMaxLength(ProductionLimits.PronunciationMaxLength);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Source).HasMaxLength(ProductionLimits.TitleMaxLength);
        builder.HasIndex(x => new { x.UserId, x.WrittenKey }).IsUnique();
    }
}
