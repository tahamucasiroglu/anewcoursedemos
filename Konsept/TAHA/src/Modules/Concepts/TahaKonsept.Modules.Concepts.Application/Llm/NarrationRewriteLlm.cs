namespace TahaKonsept.Modules.Concepts.Application.Llm;

/// <summary>
/// Yeniden yazılmış tek sahne seslendirmesi (sahne sırası + metin).
/// </summary>
public sealed record NarrationRewriteLlm
{
    /// <summary>Sahne sırası (0'dan).</summary>
    public int Index { get; init; }

    /// <summary>Yeni seslendirme.</summary>
    public string Narration { get; init; } = string.Empty;
}
