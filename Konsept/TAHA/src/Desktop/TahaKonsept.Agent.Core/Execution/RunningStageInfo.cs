using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution;

/// <summary>Çalışan aşamanın anlık durumu (tepsi menüsü/pencere gösterebilir).</summary>
/// <param name="CommandId">Komut kimliği.</param>
/// <param name="JobId">İş kimliği.</param>
/// <param name="Stage">Aşama.</param>
/// <param name="SceneIndex">Sahne indeksi.</param>
/// <param name="Percent">Yüzde.</param>
/// <param name="Message">Mesaj.</param>
/// <param name="StartedAtUtc">Başlangıç.</param>
public sealed record RunningStageInfo(
    Guid CommandId,
    Guid JobId,
    PipelineStage Stage,
    int? SceneIndex,
    double Percent,
    string Message,
    DateTimeOffset StartedAtUtc);
