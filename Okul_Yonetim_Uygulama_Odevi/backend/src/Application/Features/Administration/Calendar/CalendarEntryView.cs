using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.Calendar;

/// <summary>
/// Akademik takvim kaydı görünümü (yönetim ve okul takvimi): başlık, tür (<c>Holiday</c>, <c>Exam</c>, <c>Event</c>),
/// başlangıç ve bitiş günü (aynıysa tek günlük kayıt).
/// </summary>
public sealed record CalendarEntryView(Guid Id, Guid TermId, string Title, string Kind, DateOnly StartDate, DateOnly EndDate)
{
    public static CalendarEntryView Of(CalendarEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new(entry.Id, entry.TermId, entry.Title, entry.Kind.ToString(), entry.StartDate, entry.EndDate);
    }
}
