namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Bir modelin birim fiyatları (USD). Uygulanmayan / bilinmeyen alanlar <c>null</c>.
/// Metin: milyon token başına giriş/çıkış/önbellek; görsel: görsel başı; ses: bin karakter veya dakika başı.
/// Uzun bağlam katmanı (ör. Haiku 5.5 için 100K üstü) <see cref="LongContextThresholdTokens"/> ile tanımlanır.
/// </summary>
public sealed record ModelPricing
{
    /// <summary>Giriş, $/1M token.</summary>
    public decimal? InputPerMTok { get; init; }

    /// <summary>Çıkış, $/1M token.</summary>
    public decimal? OutputPerMTok { get; init; }

    /// <summary>Önbellekten okuma, $/1M token.</summary>
    public decimal? CacheReadPerMTok { get; init; }

    /// <summary>Önbelleğe yazma, $/1M token (null ise girişin 1.25 katı varsayılır).</summary>
    public decimal? CacheWritePerMTok { get; init; }

    /// <summary>Toplam giriş bu token sayısını aşarsa uzun bağlam fiyatı uygulanır (null = katman yok).</summary>
    public long? LongContextThresholdTokens { get; init; }

    /// <summary>Uzun bağlam giriş fiyatı, $/1M token.</summary>
    public decimal? LongContextInputPerMTok { get; init; }

    /// <summary>Uzun bağlam çıkış fiyatı, $/1M token (null ise normal çıkış fiyatı).</summary>
    public decimal? LongContextOutputPerMTok { get; init; }

    /// <summary>Görsel başı fiyat ($).</summary>
    public decimal? PerImage { get; init; }

    /// <summary>Bin karakter başı fiyat ($, TTS).</summary>
    public decimal? PerKChars { get; init; }

    /// <summary>Dakika başı fiyat ($, ses/render).</summary>
    public decimal? PerMinute { get; init; }

    /// <summary>Bin web araması başı fiyat ($, sunucu tarafı web_search aracı).</summary>
    public decimal? WebSearchPer1K { get; init; }

    /// <summary>Sağlayıcı maliyeti yanıtta kendisi bildiriyorsa (OpenRouter <c>usage.cost</c>) true.</summary>
    public bool ProviderReportsCost { get; init; }
}
