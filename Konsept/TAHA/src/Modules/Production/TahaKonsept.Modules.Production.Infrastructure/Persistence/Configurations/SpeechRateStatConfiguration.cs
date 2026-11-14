using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="SpeechRateStat"/> tablo eşlemesi (<c>production.speech_rate_stats</c>): (<c>user_id</c>, model, ses, dil) benzersiz;
/// hesaplanan <see cref="SpeechRateStat.WordsPerSecond"/> saklanmaz.
/// </summary>
public sealed class SpeechRateStatConfiguration : IEntityTypeConfiguration<SpeechRateStat>
{
    /// <summary>Eşlemeyi yapılandırır.</summary>
    /// <param name="builder">Varlık oluşturucu.</param>
    public void Configure(EntityTypeBuilder<SpeechRateStat> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("speech_rate_stats");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.SpeechModelKey).IsRequired().HasMaxLength(ProductionLimits.ModelKeyMaxLength);
        builder.Property(x => x.VoiceKey).IsRequired().HasMaxLength(ProductionLimits.ModelKeyMaxLength);
        builder.Property(x => x.Language).IsRequired().HasMaxLength(16);
        builder.Ignore(x => x.WordsPerSecond);
        builder.HasIndex(x => new { x.UserId, x.SpeechModelKey, x.VoiceKey, x.Language }).IsUnique();
    }
}
