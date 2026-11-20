using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Organization.Queries.GetSchoolDay;

/// <summary>
/// Bugünü <see cref="SchoolCalendar"/> ile (Türkiye günü) bulur; dönem kurum filtresinden okunur (görünmeyen dönem = okul
/// günü değil) ve dönemin silinmemiş takvim kayıtlarında bugüne denk gelen tatil aranır.
/// </summary>
public sealed class GetSchoolDayQueryHandler : IRequestHandler<GetSchoolDayQuery, SchoolDay>
{
    private readonly ITermRepository terms;
    private readonly ICalendarEntryRepository entries;
    private readonly SchoolCalendar calendar;

    public GetSchoolDayQueryHandler(ITermRepository terms, ICalendarEntryRepository entries, SchoolCalendar calendar)
    {
        this.terms = terms;
        this.entries = entries;
        this.calendar = calendar;
    }

    public async Task<SchoolDay> Handle(GetSchoolDayQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateOnly today = this.calendar.Today();
        Term? term = await this.terms.FindAsync(request.TermId, cancellationToken).ConfigureAwait(false);
        if (term is null || !term.Contains(today))
        {
            return new SchoolDay(today, false);
        }

        IReadOnlyList<CalendarEntry> list = await this.entries.ListByTermAsync(term.Id, cancellationToken).ConfigureAwait(false);
        return new SchoolDay(today, SchoolCalendar.IsSchoolDay(term, list, today));
    }
}
