using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Yeniden kullanım planındaki tek karar: aşama/sahne, yeni girdi hash'i ve kaynak işten yeniden kullanılıp kullanılmayacağı.
/// </summary>
public sealed record StageDecision
{
    /// <summary>Aşama.</summary>
    public PipelineStage Stage { get; init; }

    /// <summary>Sahne.</summary>
    public int? SceneIndex { get; init; }

    /// <summary>Yeni girdi hash'i.</summary>
    public string InputHash { get; init; } = string.Empty;

    /// <summary>Kaynak işten yeniden kullanılır mı.</summary>
    public bool Reuse { get; init; }

    /// <summary>Kaynak çalıştırma.</summary>
    public Guid? BaseRunId { get; init; }
}
