using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>Katalog listesi (spec 0004 Req 1–6): salt okunur; her model isteği yapan kullanıcının key'lerine göre etiketlenir.</summary>
public interface ICatalogService
{
    /// <summary>Kataloğu getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Katalog.</returns>
    Task<Result<CatalogDto>> GetAsync(CancellationToken cancellationToken);
}
