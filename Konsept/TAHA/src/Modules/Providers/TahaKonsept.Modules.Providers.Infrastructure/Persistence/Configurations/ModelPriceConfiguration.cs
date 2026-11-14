using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ModelPrice"/> tablo eşlemesi (<c>providers.model_prices</c>).
/// </summary>
public sealed class ModelPriceConfiguration : IEntityTypeConfiguration<ModelPrice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ModelPrice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("model_prices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ModelKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InputPerMTok).HasPrecision(18, 6);
        builder.Property(x => x.OutputPerMTok).HasPrecision(18, 6);
        builder.Property(x => x.CacheReadPerMTok).HasPrecision(18, 6);
        builder.Property(x => x.CacheWritePerMTok).HasPrecision(18, 6);
        builder.Property(x => x.PerImage).HasPrecision(18, 6);
        builder.Property(x => x.PerKChars).HasPrecision(18, 6);
        builder.Property(x => x.PerMinute).HasPrecision(18, 6);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.ModelKey, x.EffectiveFromUtc });
    }
}
