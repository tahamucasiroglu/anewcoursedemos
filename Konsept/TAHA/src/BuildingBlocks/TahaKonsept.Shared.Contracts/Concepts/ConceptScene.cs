namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konseptteki tek sahne: başlık, ekranda gösterilecek açıklama, görsel istemi, seslendirme metni (yazılış) ve süre ipucu.
/// </summary>
public sealed record ConceptScene
{
    /// <summary>Sahne sırası (0'dan başlar).</summary>
    public int Index { get; init; }

    /// <summary>Sahne başlığı.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Ekranda gösterilecek açıklama / metin.</summary>
    public string OnScreen { get; init; } = string.Empty;

    /// <summary>Görsel üretimi için istem (görsele yazı yazdırılmaz).</summary>
    public string VisualPrompt { get; init; } = string.Empty;

    /// <summary>Seslendirme metni (yazılış; okunuş Normalize aşamasında üretilir).</summary>
    public string Narration { get; init; } = string.Empty;

    /// <summary>Tahmini sahne süresi (saniye), opsiyonel.</summary>
    public double? DurationHintSec { get; init; }
}
