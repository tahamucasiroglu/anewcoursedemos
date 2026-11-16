using Konsept.Modules.Monitoring.Application.Dtos;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>Admin panelinin Monitoring verisi üzerindeki salt okuma sorguları (spec 0010; uygulaması Infrastructure'da).</summary>
public interface IAdminActivityRepository
{
    /// <summary>Açık hata bildirimlerini sayar (durumu Çözüldü / Yok sayıldı olmayan; bu spec anında hepsi "Yeni").</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    Task<int> CountOpenErrorReportsAsync(CancellationToken cancellationToken);

    /// <summary>Aralıktaki kullanıcılı olayların sahibini ve anını listeler.</summary>
    /// <param name="fromUtc">Başlangıç (dahil).</param>
    /// <param name="toUtc">Bitiş (hariç).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Çiftler.</returns>
    Task<IReadOnlyList<UserEventTime>> ListUserEventTimesAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    /// <summary>Kullanıcılı olayları en yenisi üstte sayfalı listeler.</summary>
    /// <param name="userIds">Yalnızca bu kullanıcılar; <c>null</c> ise hepsi.</param>
    /// <param name="fromUtc">Başlangıç (dahil); <c>null</c> sınırsız.</param>
    /// <param name="toUtc">Bitiş (hariç); <c>null</c> sınırsız.</param>
    /// <param name="skip">Atlanacak kayıt.</param>
    /// <param name="take">Alınacak kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa ve toplam.</returns>
    Task<ActivityPage> ListActivitiesAsync(
        IReadOnlyCollection<Guid>? userIds,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken);
}
