namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <c>Compose</c> aşamasının çıktısı: birleşik videonun süresi ve sahne süreleri.
/// </summary>
public sealed record ComposeOutput
{
    /// <summary>Toplam süre (sn).</summary>
    public double DurationSec { get; init; }

    /// <summary>Sahne süreleri.</summary>
    public IReadOnlyList<double> SceneDurationsSec { get; init; } = [];
}
