using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="Video"/> tablo eşlemesi (<c>production.videos</c>): maliyet <c>numeric(18,6)</c>, dökümler JSON metni; (<c>user_id</c>, <c>created_at_utc</c>) ve (<c>user_id</c>, <c>concept_id</c>) indeksleri.
/// </summary>
public sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("videos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(ProductionLimits.TitleMaxLength);
        builder.Property(x => x.Format).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.TotalCostUsd).HasPrecision(18, 6);
        builder.Property(x => x.CostBreakdownJson).IsRequired();
        builder.Property(x => x.SettingsJson).IsRequired();
        builder.Property(x => x.DocumentJson).IsRequired();
        builder.Property(x => x.ScenesJson).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.ConceptId });
    }
}
