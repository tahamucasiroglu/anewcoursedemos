using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Kullanıcının konsept üretim ayarları (<c>GET|PUT api/concepts/settings</c>): üç model (konsept + efor, konu önerisi,
/// kelime denetimi), kelime tekrar denetimi anahtarı, ince ayar (ton, izleyici, anlatım biçimi, serbest not) ve gerçek bilgi
/// (web araması). <see cref="FactGrounding"/> null ise profile göre otomatik (tarih/spor/bilim benzeri kanallarda açık);
/// sunucu <see cref="EffectiveFactGrounding"/> alanını aktif profile göre doldurur (PUT'ta yok sayılır).
/// </summary>
public sealed record ConceptSettingsDto
{
    /// <summary>Konsept üretim modeli (katalog anahtarı; varsayılan <c>claude-sonnet-5-5</c>).</summary>
    public string ConceptModelKey { get; init; } = "claude-sonnet-5-5";

    /// <summary>Konsept üretim eforu (null = model varsayılanı).</summary>
    public EffortLevel? ConceptEffort { get; init; } = EffortLevel.Medium;

    /// <summary>Konu önerisi modeli (varsayılan <c>claude-haiku-5-5</c>).</summary>
    public string TopicModelKey { get; init; } = "claude-haiku-5-5";

    /// <summary>Kelime tekrar denetimi modeli (varsayılan <c>claude-haiku-5-5</c>).</summary>
    public string WordCheckModelKey { get; init; } = "claude-haiku-5-5";

    /// <summary>Üretimden sonra kelime tekrarı denetlensin mi.</summary>
    public bool WordRepetitionCheck { get; init; }

    /// <summary>Ton (boş = profildeki).</summary>
    public string? Tone { get; init; }

    /// <summary>Hedef izleyici (boş = profildeki).</summary>
    public string? Audience { get; init; }

    /// <summary>Anlatım biçimi (boş = profildeki).</summary>
    public string? NarrativeStyle { get; init; }

    /// <summary>Serbest ince ayar notları (ör. "argo yok, her videonun sonunda soru sor").</summary>
    public string? Notes { get; init; }

    /// <summary>Gerçek bilgi / web araması (null = profile göre otomatik).</summary>
    public bool? FactGrounding { get; init; }

    /// <summary>Aktif profil için geçerli gerçek bilgi değeri (yalnız yanıtta).</summary>
    public bool EffectiveFactGrounding { get; init; }
}
