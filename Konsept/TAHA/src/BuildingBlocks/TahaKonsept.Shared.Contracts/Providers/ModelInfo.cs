namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Model kataloğundaki bir kayıt (<c>catalog.json</c> öğesi ve <c>GET api/providers/models</c> yanıt öğesi).
/// <see cref="Key"/> sistem içi sabit kimliktir; sağlayıcıya giden kimlik <see cref="ProviderModelId"/>.
/// </summary>
public sealed record ModelInfo
{
    /// <summary>Sistem içi benzersiz anahtar (ör. <c>claude-opus-5-5</c>, <c>supertonic-3</c>).</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Platform kimliği (<see cref="ProviderPlatforms"/>; yerel modellerde <c>local</c>, motorlarda <c>engine</c>).</summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>Sağlayıcı API'sine gönderilen model kimliği (OpenRouter geçiş kaydında boş: kimlik istekte verilir).</summary>
    public string ProviderModelId { get; init; } = string.Empty;

    /// <summary>Kullanıcıya görünen ad.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Yetenek.</summary>
    public ModelCapability Capability { get; init; }

    /// <summary>Çalışma yeri.</summary>
    public ModelRunsOn RunsOn { get; init; }

    /// <summary>Birim fiyatlar (yerel modellerde tümü null).</summary>
    public ModelPricing Pricing { get; init; } = new();

    /// <summary>En fazla çıkış token'ı (metin modelleri).</summary>
    public int? MaxOutputTokens { get; init; }

    /// <summary>Desteklenen efor seviyeleri (boş = efor seçimi yok).</summary>
    public IReadOnlyList<EffortLevel> EffortLevels { get; init; } = [];

    /// <summary>Varsayılan efor (null = sağlayıcı varsayılanı).</summary>
    public EffortLevel? DefaultEffort { get; init; }

    /// <summary>Dil etiketi (<see cref="LanguageLabels"/>; görsel ve video motorlarında null).</summary>
    public string? LanguageLabel { get; init; }

    /// <summary>Yerel modeller için asgari VRAM (MB; null = CPU'da çalışır / bilinmiyor).</summary>
    public int? MinVramMb { get; init; }

    /// <summary>
    /// Ses modelleri: Türkçe anlatımda varsayılan konuşma hızı (kelime/sn, 1.0× hızda). Konsept uzunluğu ve sahne planı bununla
    /// boyutlandırılır; üretilen videolardan ölçülen gerçek hız (Production) varsa o tercih edilir. null = bilinmiyor (genel varsayılan).
    /// </summary>
    public double? WordsPerSecond { get; init; }

    /// <summary>Sunucu tarafı web araması destekleniyor mu (gerçek bilgi / fact-sheet için).</summary>
    public bool SupportsWebSearch { get; init; }

    /// <summary>JSON şema ile yapılandırılmış çıktı destekleniyor mu.</summary>
    public bool SupportsStructuredOutput { get; init; }

    /// <summary>Bu modelin varsayılan olduğu amaçlar (ör. <c>concept</c>, <c>scene-code</c>, <c>topics</c>, <c>speech</c>, <c>image</c>).</summary>
    public IReadOnlyList<string> DefaultFor { get; init; } = [];

    /// <summary>Notlar (lisans, sınırlamalar, doğrulanmamış fiyat vb.).</summary>
    public string? Notes { get; init; }
}
