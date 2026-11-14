namespace TahaKonsept.Modules.Concepts.Application.Llm;

/// <summary>
/// LLM çıktısındaki sahne: başlık, ekran metni, İngilizce görsel istemi, seslendirme ve süre ipucu.
/// </summary>
public sealed record ConceptSceneLlm
{
    /// <summary>Sahne başlığı.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Ekranda görünecek kısa metin.</summary>
    public string OnScreen { get; init; } = string.Empty;

    /// <summary>İngilizce görsel istemi.</summary>
    public string VisualPrompt { get; init; } = string.Empty;

    /// <summary>Seslendirme (profil dilinde).</summary>
    public string Narration { get; init; } = string.Empty;

    /// <summary>Süre ipucu (saniye).</summary>
    public double DurationHintSec { get; init; }
}
