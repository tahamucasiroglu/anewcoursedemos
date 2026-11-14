namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Okunuş eşlemesi: yazılış → okunuş (ör. Google → Gugıl). Konsept JSON'unda LLM önerisi olarak ve sözlükte kullanılır.
/// </summary>
public sealed record PronunciationEntry
{
    /// <summary>Yazılış (ekranda/altyazıda görünen).</summary>
    public string Written { get; init; } = string.Empty;

    /// <summary>Okunuş (TTS'e giden).</summary>
    public string Spoken { get; init; } = string.Empty;
}
