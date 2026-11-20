using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Attendance;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Yoklama deposu (spec 0010): yoklama kurum + şube filtreli, satırları kurum filtreli. Yazma için izlenen okumalar
/// (yoklama ve satırları), liste için izlemesiz projeksiyon (T7).
/// </summary>
public sealed class AttendanceRepository : IAttendanceRepository
{
    private readonly OkulDbContext context;

    public AttendanceRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<AttendanceSheet?> FindSheetAsync(Guid classroomId, DateOnly day, CancellationToken cancellationToken) =>
        this.context.AttendanceSheets.FirstOrDefaultAsync(s => s.ClassroomId == classroomId && s.Day == day, cancellationToken);

    public async Task<IReadOnlyList<AttendanceEntry>> ListEntriesAsync(Guid sheetId, CancellationToken cancellationToken) =>
        await this.context.AttendanceEntries.Where(e => e.SheetId == sheetId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<Guid>> ListTakenClassroomIdsAsync(IReadOnlyCollection<Guid> classroomIds, DateOnly day, CancellationToken cancellationToken) =>
        await this.context.AttendanceSheets.AsNoTracking()
            .Where(s => s.Day == day && classroomIds.Contains(s.ClassroomId))
            .Select(s => s.ClassroomId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void AddSheet(AttendanceSheet sheet) => this.context.AttendanceSheets.Add(sheet);

    public void AddEntry(AttendanceEntry entry) => this.context.AttendanceEntries.Add(entry);
}
