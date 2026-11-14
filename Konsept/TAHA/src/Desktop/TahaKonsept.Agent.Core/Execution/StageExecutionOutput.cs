using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Execution;

/// <summary>Aşama yürütme sonucu: üretilen artefaktlar ve (varsa) çıktı JSON'u (<c>StageResultReport</c>'a aktarılır).</summary>
/// <param name="Artifacts">Artefaktlar.</param>
/// <param name="OutputJson">Çıktı JSON'u.</param>
public sealed record StageExecutionOutput(IReadOnlyList<ProducedArtifact> Artifacts, string? OutputJson = null);
