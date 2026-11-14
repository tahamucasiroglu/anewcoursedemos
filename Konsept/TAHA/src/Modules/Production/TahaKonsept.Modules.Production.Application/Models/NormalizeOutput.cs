namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <c>Normalize</c> aşamasının çıktısı: düzeltme açık mıydı ve sahne sahne yazılış/okunuş.
/// </summary>
public sealed record NormalizeOutput
{
    /// <summary>Okunuş düzeltmesi uygulandı mı.</summary>
    public bool Enabled { get; init; }

    /// <summary>Sahneler.</summary>
    public IReadOnlyList<NormalizedSegment> Segments { get; init; } = [];
}
