namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <c>ScenePlan</c> aşamasının çıktısı: sıralı sahneler ve toplam planlanan süre.
/// </summary>
public sealed record ScenePlanOutput
{
    /// <summary>Sahneler.</summary>
    public IReadOnlyList<PlannedScene> Scenes { get; init; } = [];

    /// <summary>Toplam süre (sn).</summary>
    public double TotalDurationSec { get; init; }
}
