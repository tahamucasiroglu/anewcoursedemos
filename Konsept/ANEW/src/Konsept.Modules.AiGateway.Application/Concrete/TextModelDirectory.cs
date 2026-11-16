using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Base;

namespace Konsept.Modules.AiGateway.Application.Concrete;

/// <summary>Metin modeli dizini (AiGateway.Contracts).</summary>
/// <param name="catalog">Katalog servisi.</param>
public sealed class TextModelDirectory(ICatalogService catalog) : TextModelDirectoryBase(catalog);
