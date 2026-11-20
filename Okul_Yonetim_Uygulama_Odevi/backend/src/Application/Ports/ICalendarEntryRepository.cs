using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Akademik takvim kayıtları (spec 0008) — her okuma kurum filtresinden geçer ve silinmiş kayıtları hiç göstermez (S1).
/// </summary>
public interface ICalendarEntryRepository
{
    /// <summary>Dönemin silinmemiş kayıtları, başlangıç gününe (eşitse bitişe) göre sıralı.</summary>
    Task<IReadOnlyList<CalendarEntry>> ListByTermAsync(Guid termId, CancellationToken cancellationToken);

    Task<CalendarEntry?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Dönemin, verilen tarihlerin dışında kalan (bir günü bile taşan) silinmemiş kaydı var mı.</summary>
    Task<bool> AnyOutsideAsync(Guid termId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    void Add(CalendarEntry entry);
}
