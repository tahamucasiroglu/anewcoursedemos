namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>Bir sahnenin birleşik ses içindeki zamanı.</summary>
public sealed record SpeechSceneTiming
{
    /// <summary>Sahne indeksi.</summary>
    public int SceneIndex { get; init; }

    /// <summary>Başlangıç (sn).</summary>
    public double StartSec { get; init; }

    /// <summary>Süre (sn).</summary>
    public double DurationSec { get; init; }

    /// <summary>Sahnenin ayrı ses dosyası (ör. <c>scene-0.wav</c>).</summary>
    public string FileName { get; init; } = string.Empty;
}
