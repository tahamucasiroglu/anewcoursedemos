using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration.Calendar;
using OkulYonetim.Application.Features.Administration.Terms;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Organization.Queries.GetSchoolCalendar;

/// <summary>
/// Güncel dönemi Türkiye gününe göre seçer (<see cref="SchoolCalendar"/>) ve silinmemiş takvim kayıtlarını döndürür.
/// Okumalar kurum filtresinden geçer: yalnızca seçili okulun dönemi ve kayıtları görünür (S1).
/// </summary>
public sealed class GetSchoolCalendarQueryHandler : IRequestHandler<GetSchoolCalendarQuery, SchoolCalendarView>
{
    private readonly ITermRepository terms;
    private readonly ICalendarEntryRepository entries;
    private readonly SchoolCalendar calendar;

    public GetSchoolCalendarQueryHandler(ITermRepository terms, ICalendarEntryRepository entries, SchoolCalendar calendar)
    {
        this.terms = terms;
        this.entries = entries;
        this.calendar = calendar;
    }

    public async Task<SchoolCalendarView> Handle(GetSchoolCalendarQuery request, CancellationToken cancellationToken)
    {
        DateOnly today = this.calendar.Today();
        Term? current = SchoolCalendar.PickCurrent(await this.terms.ListAsync(cancellationToken).ConfigureAwait(false), today);
        if (current is null)
        {
            return new SchoolCalendarView(null, []);
        }

        IReadOnlyList<CalendarEntry> list = await this.entries.ListByTermAsync(current.Id, cancellationToken).ConfigureAwait(false);
        return new SchoolCalendarView(TermAdminView.Of(current, current, today), [.. list.Select(CalendarEntryView.Of)]);
    }
}
