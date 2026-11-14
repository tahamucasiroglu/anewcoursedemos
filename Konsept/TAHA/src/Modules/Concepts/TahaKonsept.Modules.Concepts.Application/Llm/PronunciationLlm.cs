namespace TahaKonsept.Modules.Concepts.Application.Llm;

/// <summary>
/// LLM çıktısındaki okunuş önerisi (yazılış → okunuş).
/// </summary>
public sealed record PronunciationLlm
{
    /// <summary>Yazılış.</summary>
    public string Written { get; init; } = string.Empty;

    /// <summary>Okunuş.</summary>
    public string Spoken { get; init; } = string.Empty;
}
