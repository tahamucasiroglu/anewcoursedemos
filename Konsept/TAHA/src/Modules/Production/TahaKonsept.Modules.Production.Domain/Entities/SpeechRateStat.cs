using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Production.Domain.Constants;

namespace TahaKonsept.Modules.Production.Domain.Entities;

/// <summary>
/// Ölçülmüş konuşma hızı (kullanıcı + ses modeli + ses + dil başına): üretilen her sesli videoda yazılış kelime sayısı ve
/// seslendirme süresi eklenir; <see cref="WordsPerSecond"/> = toplam kelime / toplam süre (1.0× hıza indirgenmiş).
/// <see cref="AddSample"/> en fazla <see cref="ProductionLimits.MaxSpeechRateSamples"/> örnek ağırlığı tutar (eskiler sönümlenir → kendini ayarlar).
/// </summary>
public sealed class SpeechRateStat : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için boş kurucu.</summary>
    private SpeechRateStat()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Ses modeli katalog anahtarı.</summary>
    public string SpeechModelKey { get; private set; } = string.Empty;

    /// <summary>Ses kimliği (boş = modelin varsayılan sesi).</summary>
    public string VoiceKey { get; private set; } = string.Empty;

    /// <summary>Dil (küçük harf, ör. <c>tr</c>).</summary>
    public string Language { get; private set; } = "tr";

    /// <summary>Örnek ağırlığı (sönümlemeyle en fazla <see cref="ProductionLimits.MaxSpeechRateSamples"/>).</summary>
    public double SampleWeight { get; private set; }

    /// <summary>Toplam (ağırlıklı) yazılış kelimesi.</summary>
    public double TotalWords { get; private set; }

    /// <summary>Toplam (ağırlıklı) seslendirme süresi, 1.0× hıza indirgenmiş (sn).</summary>
    public double TotalSeconds { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Ortalama hız (kelime/sn, 1.0×); veri yoksa 0.</summary>
    public double WordsPerSecond => TotalSeconds > 0 ? TotalWords / TotalSeconds : 0;

    /// <summary>Boş kayıt oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="speechModelKey">Ses modeli.</param>
    /// <param name="voiceKey">Ses (null/boş = varsayılan).</param>
    /// <param name="language">Dil.</param>
    /// <returns>Kayıt.</returns>
    public static SpeechRateStat Create(Guid userId, string speechModelKey, string? voiceKey, string language) => new()
    {
        UserId = userId,
        SpeechModelKey = ProductionLimits.Clip(speechModelKey, ProductionLimits.ModelKeyMaxLength),
        VoiceKey = ProductionLimits.Clip(voiceKey, ProductionLimits.ModelKeyMaxLength),
        Language = ProductionLimits.Clip(string.IsNullOrWhiteSpace(language) ? "tr" : language.ToLowerInvariant(), 16),
    };

    /// <summary>Bir videonun ölçümünü ekler. Ağırlık sınıra ulaştıysa eski toplamlar orantılı küçültülür.</summary>
    /// <param name="words">Yazılış kelime sayısı (&gt; 0).</param>
    /// <param name="baseSeconds">1.0× hıza indirgenmiş seslendirme süresi (sn, &gt; 0).</param>
    public void AddSample(int words, double baseSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(words);
        if (double.IsNaN(baseSeconds) || double.IsInfinity(baseSeconds) || baseSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseSeconds));
        }

        if (SampleWeight >= ProductionLimits.MaxSpeechRateSamples)
        {
            var keep = (ProductionLimits.MaxSpeechRateSamples - 1) / SampleWeight;
            TotalWords *= keep;
            TotalSeconds *= keep;
            SampleWeight = ProductionLimits.MaxSpeechRateSamples - 1;
        }

        TotalWords += words;
        TotalSeconds += baseSeconds;
        SampleWeight += 1;
    }
}
