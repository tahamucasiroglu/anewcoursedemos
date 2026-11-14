namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Sahne planındaki tek sahne: metinler, görsel istemi, planlanan süre (seslendirme uzunluğundan) ve görsel dosya adları.
/// </summary>
public sealed record PlannedScene
{
    /// <summary>Sahne indeksi (0'dan).</summary>
    public int Index { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Ekranda gösterilecek metin.</summary>
    public string OnScreen { get; init; } = string.Empty;

    /// <summary>Seslendirme yazılışı.</summary>
    public string Narration { get; init; } = string.Empty;

    /// <summary>Görsel istemi.</summary>
    public string VisualPrompt { get; init; } = string.Empty;

    /// <summary>Planlanan süre (sn).</summary>
    public double DurationSec { get; init; }

    /// <summary>Sahnenin görsel dosya adları (görsel kapalıysa boş).</summary>
    public IReadOnlyList<string> ImageNames { get; init; } = [];
}
