using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Paging;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Models;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Abstract;

/// <summary>
/// Günlük okuma/yazma uç servisi (API controller'ı kullanır). Diğer modüller yazmak için <see cref="IActivityLogger"/> kullanır.
/// </summary>
public interface IActivityLogService : IApplicationService
{
    /// <summary>Şerit için son kayıtlar (eskiden yeniye).</summary>
    /// <param name="afterId">Son görülen kayıt.</param>
    /// <param name="take">En çok kayıt (1–200, varsayılan 50).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<Result<IReadOnlyList<ActivityLogDto>>> GetRecentAsync(Guid? afterId, int? take, CancellationToken cancellationToken = default);

    /// <summary>Filtreli, sayfalı arama (en yeni önce).</summary>
    /// <param name="query">Filtre.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    Task<Result<PagedResult<ActivityLogDto>>> SearchAsync(ActivityLogQuery query, CancellationToken cancellationToken = default);

    /// <summary>İstemci olayını (Web / ajan) oturum kullanıcısı adına yazar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oluşan kayıt.</returns>
    Task<Result<ActivityLogDto>> CreateAsync(ActivityLogCreateRequest request, CancellationToken cancellationToken = default);
}
