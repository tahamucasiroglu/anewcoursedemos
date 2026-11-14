using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// İş ayrıntısı sorgusu (<c>GET api/production/jobs/{id}</c>).
/// </summary>
public interface IJobQueryService
{
    /// <summary>İş ayrıntısı.</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<JobDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken);
}
