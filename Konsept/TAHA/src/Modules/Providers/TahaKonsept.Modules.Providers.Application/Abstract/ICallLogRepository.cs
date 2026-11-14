using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Providers.Domain.Entities;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Çağrı günlüğü deposu (harcama toplamları).
/// </summary>
public interface ICallLogRepository : IRepository<CallLog>
{
    /// <summary>Aralıktaki harcamayı platforma göre toplar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="fromUtc">Başlangıç (dahil).</param>
    /// <param name="toUtc">Bitiş (hariç).</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<IReadOnlyList<PlatformUsage>> SumByPlatformAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
}
