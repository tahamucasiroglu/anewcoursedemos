using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>Admin paneli İstatistikler ve Aktiviteler (spec 0010). Yetki (<c>admin.panel</c>) uçtadır.</summary>
public interface IAdminStatisticsService
{
    /// <summary>Özet sayılar ve günlük seri.</summary>
    /// <param name="days">Aralık (7 / 30 / 90; başka değer 30).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstatistikler.</returns>
    Task<Result<AdminStatisticsDto>> GetStatisticsAsync(int? days, CancellationToken cancellationToken);

    /// <summary>Aktivitelerin bir sayfası.</summary>
    /// <param name="request">Sayfa ve filtreler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    Task<Result<AdminActivityPageDto>> GetActivitiesAsync(AdminActivitiesRequest? request, CancellationToken cancellationToken);
}
