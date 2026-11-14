namespace TahaKonsept.Modules.Concepts.Application.Llm;

/// <summary>
/// Konsept modelinin yapılandırılmış JSON çıktısı (şema bu türden üretilir; <c>IProviderGateway.RunStructuredAsync</c>).
/// Kanal alanları (dil, biçim, süre) bilerek yoktur: onları sistem belirler. Belgeye çevirme: <c>ConceptGeneratorBase.BuildDocument</c>.
/// </summary>
public sealed record ConceptLlmOutput
{
    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Kanca cümlesi.</summary>
    public string Hook { get; init; } = string.Empty;

    /// <summary>Tek cümle özet.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Sahneler.</summary>
    public List<ConceptSceneLlm> Scenes { get; init; } = [];

    /// <summary>Kaynaklar.</summary>
    public List<ConceptSourceLlm> Sources { get; init; } = [];

    /// <summary>Okunuşlar.</summary>
    public List<PronunciationLlm> Pronunciations { get; init; } = [];

    /// <summary>Etiketler.</summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>Kontrol edilmesi gereken iddialar (yoksa boş).</summary>
    public List<string> ReviewNotes { get; init; } = [];
}
