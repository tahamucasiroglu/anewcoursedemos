namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Video ayrıntısındaki sahne: başlık, ekran metni, yazılış (altyazı) ve okunuş (ses), süre.
/// </summary>
public sealed record VideoSceneDto
{
    /// <summary>Sahne indeksi.</summary>
    public int Index { get; init; }

    /// <summary>Sahne başlığı.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Ekranda gösterilen açıklama.</summary>
    public string OnScreen { get; init; } = string.Empty;

    /// <summary>Seslendirme yazılışı (altyazı).</summary>
    public string DisplayText { get; init; } = string.Empty;

    /// <summary>Okunuş (TTS'e giden).</summary>
    public string SpokenText { get; init; } = string.Empty;

    /// <summary>Videodaki başlangıç (sn).</summary>
    public double StartSec { get; init; }

    /// <summary>Süre (sn).</summary>
    public double DurationSec { get; init; }
}
