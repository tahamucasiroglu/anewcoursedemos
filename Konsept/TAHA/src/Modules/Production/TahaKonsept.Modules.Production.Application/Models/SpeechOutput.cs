using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <c>Speech</c> aşamasının çıktısı: kip, sessiz mi, toplam süre, sahne zamanlamaları ve (online ise) birleşik ses artefaktının adı.
/// </summary>
public sealed record SpeechOutput
{
    /// <summary>Kip.</summary>
    public MediaMode Mode { get; init; }

    /// <summary>Sessiz video mu (ses kapalı).</summary>
    public bool Silent { get; init; }

    /// <summary>Toplam süre (sn).</summary>
    public double TotalDurationSec { get; init; }

    /// <summary>Sahne zamanlamaları.</summary>
    public IReadOnlyList<SpeechSceneTiming> Scenes { get; init; } = [];

    /// <summary>Online birleşik ses artefaktı adı.</summary>
    public string? AudioArtifactName { get; init; }
}
