namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Normalize çıktısındaki sahne metni: yazılış (altyazı) ve okunuş (TTS).
/// </summary>
public sealed record NormalizedSegment
{
    /// <summary>Sahne.</summary>
    public int SceneIndex { get; init; }

    /// <summary>Yazılış.</summary>
    public string DisplayText { get; init; } = string.Empty;

    /// <summary>Okunuş.</summary>
    public string SpokenText { get; init; } = string.Empty;

    /// <summary>Yapılan değişiklik sayısı.</summary>
    public int ChangeCount { get; init; }
}
