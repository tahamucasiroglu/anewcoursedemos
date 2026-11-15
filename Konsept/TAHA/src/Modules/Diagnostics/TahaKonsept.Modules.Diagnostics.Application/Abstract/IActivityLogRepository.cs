using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Abstract;

/// <summary>
/// <see cref="ActivityLog"/> deposu: genel <see cref="IRepository{TEntity}"/> + şerit için "son kayıtlar" ve seviye sayımı.
/// </summary>
public interface IActivityLogRepository : IRepository<ActivityLog>
{
    /// <summary>
    /// Son kayıtlar (eskiden yeniye sıralı). <paramref name="afterId"/> verilir ve bulunursa yalnızca o kayıttan sonra (aynı anda
    /// oluşturulanlar dahil, kendisi hariç) gelenler döner; bulunamazsa en son <paramref name="take"/> kayıt.
    /// </summary>
    /// <param name="afterId">Son görülen kayıt.</param>
    /// <param name="take">En çok kayıt.</param>
    /// <param name="ownerId">Yalnızca bu kullanıcının kayıtları (sistem/başka kullanıcı kayıtları hariç; <see cref="Guid.Empty"/> hiçbir şey döndürmez); <c>null</c> ise hepsi (yönetici).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<ActivityLog>> GetRecentAsync(Guid? afterId, int take, Guid? ownerId, CancellationToken cancellationToken = default);

    /// <summary>Seviyeye göre kayıt sayıları.</summary>
    /// <param name="sinceUtc">Bu andan sonrakiler; <c>null</c> ise tümü.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Seviye → sayı (kaydı olmayan seviye yer almaz).</returns>
    Task<IReadOnlyDictionary<LogLevelKind, int>> CountByLevelAsync(DateTimeOffset? sinceUtc, CancellationToken cancellationToken = default);
}
