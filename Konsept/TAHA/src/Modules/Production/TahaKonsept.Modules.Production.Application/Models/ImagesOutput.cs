using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <c>Images</c> aşamasının çıktısı: kip ve görseller.
/// </summary>
public sealed record ImagesOutput
{
    /// <summary>Kip.</summary>
    public MediaMode Mode { get; init; }

    /// <summary>Görseller.</summary>
    public IReadOnlyList<GeneratedImage> Images { get; init; } = [];
}
