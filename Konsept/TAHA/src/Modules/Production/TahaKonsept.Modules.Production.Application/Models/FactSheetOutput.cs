using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <c>FactSheet</c> aşamasının çıktısı: konsept kaynaklarından derlenen bilgi listesi (kaynak yoksa kullanılmaz; geçiş aşaması).
/// </summary>
public sealed record FactSheetOutput
{
    /// <summary>Kaynak var mıydı.</summary>
    public bool Used { get; init; }

    /// <summary>Kaynaklar.</summary>
    public IReadOnlyList<ConceptSource> Sources { get; init; } = [];
}
