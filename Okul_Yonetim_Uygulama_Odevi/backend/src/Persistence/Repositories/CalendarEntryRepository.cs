using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Akademik takvim deposu: her okuma kurum filtresinden geçer; silinmiş kayıt filtrede elenir.</summary>
public sealed class CalendarEntryRepository : ICalendarEntryRepository
{
    private readonly OkulDbContext context;

    public CalendarEntryRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<CalendarEntry>> ListByTermAsync(Guid termId, CancellationToken cancellationToken) =>
        await this.context.CalendarEntries.Where(e => e.TermId == termId)
            .OrderBy(e => e.StartDate).ThenBy(e => e.EndDate).ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<CalendarEntry?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.CalendarEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> AnyOutsideAsync(Guid termId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        this.context.CalendarEntries.AnyAsync(e => e.TermId == termId && (e.StartDate < startDate || e.EndDate > endDate), cancellationToken);

    public void Add(CalendarEntry entry) => this.context.CalendarEntries.Add(entry);
}
