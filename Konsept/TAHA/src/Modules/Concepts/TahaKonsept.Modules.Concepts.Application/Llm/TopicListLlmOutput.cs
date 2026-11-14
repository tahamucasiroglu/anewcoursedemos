namespace TahaKonsept.Modules.Concepts.Application.Llm;

/// <summary>
/// Konu önerisi modelinin JSON çıktısı: yalnızca başlık + tek satırlık kanca listesi (token tasarrufu).
/// </summary>
public sealed record TopicListLlmOutput
{
    /// <summary>Konular.</summary>
    public List<TopicLlmItem> Topics { get; init; } = [];
}
