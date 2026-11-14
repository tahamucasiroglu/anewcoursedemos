namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>Altyazı kelimesi (yazılış) ve zamanı (ms).</summary>
public sealed record CaptionWordItem
{
    /// <summary>Kelime.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Başlangıç (ms).</summary>
    public int StartMs { get; init; }

    /// <summary>Bitiş (ms).</summary>
    public int EndMs { get; init; }
}
