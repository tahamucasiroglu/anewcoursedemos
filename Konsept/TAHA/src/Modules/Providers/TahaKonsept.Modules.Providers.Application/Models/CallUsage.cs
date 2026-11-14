namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Bir çağrının ölçülen kullanımı: token'lar (önbellek dahil), web araması, görsel / karakter / ses süresi birimleri ve
/// sağlayıcının kendisi bildirdiyse maliyet (OpenRouter <c>usage.cost</c>). <see cref="InputTokens"/> önbellek DIŞI giriştir.
/// Önemli üyeler: <see cref="Empty"/>, <see cref="Add"/>, <see cref="TotalInputTokens"/>.
/// </summary>
public sealed record CallUsage
{
    /// <summary>Boş kullanım.</summary>
    public static CallUsage Empty { get; } = new();

    /// <summary>Önbellek dışı giriş token'ı.</summary>
    public long InputTokens { get; init; }

    /// <summary>Çıkış token'ı (düşünme dahil).</summary>
    public long OutputTokens { get; init; }

    /// <summary>Önbellekten okunan giriş token'ı.</summary>
    public long CacheReadTokens { get; init; }

    /// <summary>Önbelleğe yazılan giriş token'ı.</summary>
    public long CacheWriteTokens { get; init; }

    /// <summary>Sunucu tarafı web araması sayısı.</summary>
    public int WebSearchRequests { get; init; }

    /// <summary>Üretilen görsel sayısı.</summary>
    public int Images { get; init; }

    /// <summary>Seslendirilen karakter sayısı.</summary>
    public long Characters { get; init; }

    /// <summary>Üretilen ses süresi (saniye).</summary>
    public decimal AudioSeconds { get; init; }

    /// <summary>Sağlayıcının bildirdiği maliyet ($; bildirmiyorsa null).</summary>
    public decimal? ProviderCostUsd { get; init; }

    /// <summary>Toplam giriş (önbellek dahil); uzun bağlam katmanı bu değere göre seçilir.</summary>
    public long TotalInputTokens => InputTokens + CacheReadTokens + CacheWriteTokens;

    /// <summary>İki kullanımı toplar (ör. JSON onarım çağrısı ile asıl çağrı).</summary>
    /// <param name="other">Diğer kullanım.</param>
    /// <returns>Toplam.</returns>
    public CallUsage Add(CallUsage other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new CallUsage
        {
            InputTokens = InputTokens + other.InputTokens,
            OutputTokens = OutputTokens + other.OutputTokens,
            CacheReadTokens = CacheReadTokens + other.CacheReadTokens,
            CacheWriteTokens = CacheWriteTokens + other.CacheWriteTokens,
            WebSearchRequests = WebSearchRequests + other.WebSearchRequests,
            Images = Images + other.Images,
            Characters = Characters + other.Characters,
            AudioSeconds = AudioSeconds + other.AudioSeconds,
            ProviderCostUsd = ProviderCostUsd is null && other.ProviderCostUsd is null
                ? null
                : (ProviderCostUsd ?? 0) + (other.ProviderCostUsd ?? 0),
        };
    }
}
