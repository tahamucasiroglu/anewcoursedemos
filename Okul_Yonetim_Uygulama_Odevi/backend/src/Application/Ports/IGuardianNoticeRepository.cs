using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Veli bildirimleri (spec 0010). Kurum filtreli; velinin göreceği bildirimler yalnızca oturumdaki kişinin ve bağlı olduğu
/// öğrencilerinkilerdir (öğrenci listesi çağırandan — bağ üzerinden — gelir; plan Risks 6).
/// </summary>
public interface IGuardianNoticeRepository
{
    /// <summary>Verilen öğrencilerin o günkü bildirimleri (bütün veliler) — tekrar bildirim engeli ve düzeltme için.</summary>
    Task<IReadOnlyList<GuardianNotice>> ListForStudentsOnDayAsync(IReadOnlyCollection<Guid> studentIds, DateOnly day, CancellationToken cancellationToken);

    /// <summary>Velinin, verilen öğrencilere ait bildirimleri: yeniden eskiye, en fazla <paramref name="max"/>.</summary>
    Task<IReadOnlyList<GuardianNotice>> ListForGuardianAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, int max, CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<GuardianNotice>> ListUnreadAsync(Guid guardianUserId, CancellationToken cancellationToken);

    void Add(GuardianNotice notice);
}
