namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>Hizalama segmenti: yazılış, okunuş ve (biliniyorsa) ses içindeki zaman aralığı.</summary>
public sealed record AlignSegmentItem
{
    /// <summary>Sahne indeksi.</summary>
    public int SceneIndex { get; init; }

    /// <summary>Yazılış (altyazı).</summary>
    public string DisplayText { get; init; } = string.Empty;

    /// <summary>Okunuş (TTS).</summary>
    public string SpokenText { get; init; } = string.Empty;

    /// <summary>Başlangıç (sn) ya da <c>null</c>.</summary>
    public double? StartSec { get; init; }

    /// <summary>Bitiş (sn) ya da <c>null</c>.</summary>
    public double? EndSec { get; init; }
}
