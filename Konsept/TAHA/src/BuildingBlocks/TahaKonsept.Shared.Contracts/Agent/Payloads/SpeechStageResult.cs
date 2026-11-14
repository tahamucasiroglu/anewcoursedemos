namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>
/// <c>Speech</c> aşamasının ajan çıktısı (<c>StageResultReport.OutputJson</c>): birleşik ses dosyası ve sahne zamanlamaları.
/// Sunucu sahne sürelerini <c>SceneRender</c>/<c>Compose</c> girdisinde kullanır.
/// </summary>
public sealed record SpeechStageResult
{
    /// <summary>Birleşik ses dosyasının adı (<see cref="AgentArtifactNames.SpeechAudio"/>).</summary>
    public string AudioFileName { get; init; } = AgentArtifactNames.SpeechAudio;

    /// <summary>Toplam süre (sn).</summary>
    public double TotalDurationSec { get; init; }

    /// <summary>Sahne zamanlamaları (sıralı).</summary>
    public IReadOnlyList<SpeechSceneTiming> Scenes { get; init; } = [];
}
