using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Base;

namespace Konsept.Modules.AiGateway.Application.Concrete;

/// <summary>Tüm katalog girişleri dizini (AiGateway.Contracts, spec 0006).</summary>
/// <param name="catalog">Katalog servisi.</param>
public sealed class ModelDirectory(ICatalogService catalog) : ModelDirectoryBase(catalog);
