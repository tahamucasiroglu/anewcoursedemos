using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>Sistemle gelen sürümlenmiş katalog seed'i (spec 0004 Req 5; uygulaması Infrastructure'da gömülü dosya).</summary>
public interface ICatalogSource
{
    /// <summary>Gets katalog.</summary>
    CatalogSeed Catalog { get; }
}
