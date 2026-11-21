using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Veli bildirimleri (spec 0010). Kurum filtreli; velinin göreceği bildirimler yalnızca oturumdaki kişinin ve bağlı olduğu
/// öğrencilerinkilerdir (öğrenci listesi çağırandan — bağ üzerinden — gelir; plan Risks 6).
/// </summary>
public interface IGuardianNoticeRepository
{
    /// <summary>Verilen öğrencilerin o günkü yoklama bildirimleri (bütün veliler; kayda bağlı okul bildirimleri hariç — spec 0013) — tekrar bildirim engeli ve düzeltme için.</summary>
    Task<IReadOnlyList<GuardianNotice>> ListForStudentsOnDayAsync(IReadOnlyCollection<Guid> studentIds, DateOnly day, CancellationToken cancellationToken);

    /// <summary>Velinin, verilen öğrencilere ait bildirimleri: yeniden eskiye, en fazla <paramref name="max"/>.</summary>
    Task<IReadOnlyList<GuardianNotice>> ListForGuardianAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, int max, CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken);

    /// <summary>
    /// Velinin, verilen öğrencilere ait, okunmamış ve son değişikliği <paramref name="upTo"/>'dan (UTC) sonra olmayan bütün
    /// bildirimleri (izlenen — okundu yapmak için; 0010 review F-3 ve review 2, bulgu 1).
    /// </summary>
    Task<IReadOnlyList<GuardianNotice>> ListUnreadUpToAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, DateTimeOffset upTo, CancellationToken cancellationToken);

    /// <summary>Velinin, verilen öğrencilerinden birine ait tek bildirimi (izlenen — ayrıntıda okundu yapmak için; spec 0014); yoksa null.</summary>
    Task<GuardianNotice?> FindForGuardianAsync(Guid id, Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken);

    void Add(GuardianNotice notice);
}
