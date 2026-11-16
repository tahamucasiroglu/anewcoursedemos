using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Testin verdiği katalog seed'i (AC-2: değer yalnızca seed değiştirilerek değişir).</summary>
/// <param name="catalog">Seed.</param>
public sealed class FixedCatalogSource(CatalogSeed catalog) : ICatalogSource
{
    /// <inheritdoc />
    public CatalogSeed Catalog { get; } = catalog;
}
