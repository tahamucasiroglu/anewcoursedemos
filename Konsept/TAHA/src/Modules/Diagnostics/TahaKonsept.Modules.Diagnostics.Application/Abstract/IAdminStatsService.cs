using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Abstract;

/// <summary>
/// Yönetim paneli istatistikleri: kayıtlı tüm <see cref="IStatsContributor"/>'ları çağırıp tek yanıtta birleştirir (yönetici).
/// </summary>
public interface IAdminStatsService : IApplicationService
{
    /// <summary>İstatistikleri hesaplar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstatistikler.</returns>
    Task<Result<AdminStatsResponse>> GetAsync(CancellationToken cancellationToken = default);
}
