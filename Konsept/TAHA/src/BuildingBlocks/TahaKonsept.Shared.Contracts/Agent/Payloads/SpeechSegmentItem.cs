namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>Bir sahnenin seslendirme metni: yazılış (ekran) ve okunuş (TTS).</summary>
public sealed record SpeechSegmentItem
{
    /// <summary>Sahne indeksi.</summary>
    public int SceneIndex { get; init; }

    /// <summary>Yazılış (altyazı).</summary>
    public string DisplayText { get; init; } = string.Empty;

    /// <summary>Okunuş (TTS'e giden metin; Normalize çıktısı).</summary>
    public string SpokenText { get; init; } = string.Empty;
}
