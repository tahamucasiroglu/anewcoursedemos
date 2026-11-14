namespace TahaKonsept.Modules.Concepts.Application.Llm;

/// <summary>
/// LLM çıktısındaki kaynak (başlık + URL).
/// </summary>
public sealed record ConceptSourceLlm
{
    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Tam URL.</summary>
    public string Url { get; init; } = string.Empty;
}
