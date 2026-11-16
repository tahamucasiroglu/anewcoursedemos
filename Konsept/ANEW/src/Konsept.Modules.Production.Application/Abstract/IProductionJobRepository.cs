using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>Üretim işlerinin kalıcılığı; her işlem kullanıcı kimliğiyle süzülür (BR-1). Kullanıcı başına en çok bir çalışan iş (DB kısıtı).</summary>
public interface IProductionJobRepository
{
    /// <summary>Kullanıcının tamamlanmamış işleri (tüm profiller).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İşler.</returns>
    Task<IReadOnlyList<ProductionJob>> ListActiveAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının işi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş ya da <c>null</c>.</returns>
    Task<ProductionJob?> GetAsync(Guid userId, Guid jobId, CancellationToken cancellationToken);

    /// <summary>Profil sırasındaki en büyük konum (boşsa 0).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Konum.</returns>
    Task<long> MaxPositionAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>İş ekler.</summary>
    /// <param name="job">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(ProductionJob job, CancellationToken cancellationToken);

    /// <summary>İki işin konumunu, ikisi de <paramref name="movable"/> durumundaysa değiş tokuş eder (tek işlem).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="first">Birinci iş.</param>
    /// <param name="second">İkinci iş.</param>
    /// <param name="movable">Taşınabilir durumlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Değiştiyse <c>true</c>.</returns>
    Task<bool> SwapAsync(
        Guid userId, ProductionJob first, ProductionJob second, IReadOnlyCollection<ProductionJobStatus> movable, CancellationToken cancellationToken);

    /// <summary>İşi siler; yalnızca <paramref name="allowed"/> durumundaysa.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="allowed">İzin verilen durumlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silindiyse <c>true</c>.</returns>
    Task<bool> RemoveAsync(Guid userId, Guid jobId, IReadOnlyCollection<ProductionJobStatus> allowed, CancellationToken cancellationToken);

    /// <summary>
    /// İşi <i>Üretiliyor</i> yapar ve ayarları sabitler; yalnızca <paramref name="from"/> durumundaysa ve kullanıcının başka çalışan işi yoksa.
    /// </summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="from">Beklenen durumlar.</param>
    /// <param name="fixedSettings">Sabitlenecek ayarlar.</param>
    /// <param name="at">Zaman (UTC).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Başladıysa <c>true</c>.</returns>
    Task<bool> StartAsync(
        Guid userId,
        Guid jobId,
        IReadOnlyCollection<ProductionJobStatus> from,
        FixedJobSettings fixedSettings,
        DateTimeOffset at,
        CancellationToken cancellationToken);

    /// <summary>Çalışan işleri (<i>Üretiliyor</i>, <i>Ajan bekleniyor</i>) <i>Durduruldu</i> yapar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Yalnızca bu profil; <c>null</c> = tüm profiller (çıkış).</param>
    /// <param name="at">Zaman (UTC).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durdurulan sayı.</returns>
    Task<int> StopAsync(Guid userId, Guid? profileId, DateTimeOffset at, CancellationToken cancellationToken);
}
