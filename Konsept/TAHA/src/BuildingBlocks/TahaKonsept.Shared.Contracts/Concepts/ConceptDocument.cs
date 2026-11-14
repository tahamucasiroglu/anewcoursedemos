namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konseptin tam JSON modeli (MIMARI §6): başlık, özet, dil, biçim, hedef süre, ton/izleyici/anlatım,
/// sahneler (<see cref="Scenes"/>), kaynaklar, okunuş önerileri ve etiketler. LLM çıktısı bu şemaya göre üretilir.
/// </summary>
public sealed record ConceptDocument
{
    /// <summary>Video başlığı.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>İlk 2 saniyede söylenen / gösterilen kanca cümlesi (eklendi: dalga 2; eski belgelerde boş).</summary>
    public string Hook { get; init; } = string.Empty;

    /// <summary>Kısa özet.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Video dili (BCP-47, ör. <c>tr</c>).</summary>
    public string Language { get; init; } = "tr";

    /// <summary>Biçim (dikey/yatay).</summary>
    public VideoFormat Format { get; init; } = VideoFormat.Vertical;

    /// <summary>Hedef süre (saniye).</summary>
    public int TargetDurationSec { get; init; } = 60;

    /// <summary>Anlatım tonu.</summary>
    public string Tone { get; init; } = string.Empty;

    /// <summary>Hedef izleyici.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Anlatım biçimi.</summary>
    public string NarrativeStyle { get; init; } = string.Empty;

    /// <summary>Sahneler (sıralı).</summary>
    public IReadOnlyList<ConceptScene> Scenes { get; init; } = [];

    /// <summary>Kaynaklar.</summary>
    public IReadOnlyList<ConceptSource> Sources { get; init; } = [];

    /// <summary>Okunuş önerileri (yazılış → okunuş).</summary>
    public IReadOnlyList<PronunciationEntry> Pronunciations { get; init; } = [];

    /// <summary>Etiketler.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Yayından önce kontrol edilmesi gereken (modelin emin olmadığı) iddialar — özellikle web araması olmayan modellerde.</summary>
    public IReadOnlyList<string> ReviewNotes { get; init; } = [];
}
