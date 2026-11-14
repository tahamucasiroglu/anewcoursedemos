namespace TahaKonsept.Modules.Concepts.Application.Llm;

/// <summary>
/// LLM'in önerdiği tek konu.
/// </summary>
public sealed record TopicLlmItem
{
    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Tek satırlık kanca.</summary>
    public string Hook { get; init; } = string.Empty;
}
