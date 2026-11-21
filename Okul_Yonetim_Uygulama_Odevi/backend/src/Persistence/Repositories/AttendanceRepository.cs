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

    public async Task<IReadOnlyList<(DateOnly Day, AttendanceStatus Status)>> ListStudentDaysAsync(Guid studentId, DateOnly fromDay, DateOnly toDay, CancellationToken cancellationToken)
    {
        var rows = await (
            from entry in this.context.AttendanceEntries.AsNoTracking()
            join sheet in this.context.AttendanceSheets.AsNoTracking() on entry.SheetId equals sheet.Id
            where entry.StudentId == studentId && sheet.Day >= fromDay && sheet.Day <= toDay
            select new { sheet.Day, sheet.UpdatedAt, entry.Status })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.GroupBy(r => r.Day).Select(g => g.OrderByDescending(r => r.UpdatedAt).First()).Select(r => (r.Day, r.Status))];
    }

    public async Task<IReadOnlyList<(Guid ClassroomId, Guid StudentId, AttendanceStatus Status)>> ListDayExceptionsAsync(IReadOnlyCollection<Guid> classroomIds, DateOnly day, CancellationToken cancellationToken) =>
        [.. (await (from entry in this.context.AttendanceEntries.AsNoTracking()
                    join sheet in this.context.AttendanceSheets.AsNoTracking() on entry.SheetId equals sheet.Id
                    where sheet.Day == day && classroomIds.Contains(sheet.ClassroomId) && entry.Status != AttendanceStatus.Present
                    select new { sheet.ClassroomId, entry.StudentId, entry.Status })
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(x => (x.ClassroomId, x.StudentId, x.Status))];

    public async Task<IReadOnlyList<(Guid StudentId, DateOnly Day, AttendanceStatus Status)>> ListStatusDaysAsync(DateOnly fromDay, DateOnly toDay, Guid? branchId, Guid? classroomId, CancellationToken cancellationToken)
    {
        IQueryable<AttendanceSheet> sheets = this.context.AttendanceSheets.AsNoTracking().Where(s => s.Day >= fromDay && s.Day <= toDay);
        if (branchId is { } branch)
        {
            sheets = sheets.Where(s => s.BranchId == branch);
        }

        if (classroomId is { } classroom)
        {
            sheets = sheets.Where(s => s.ClassroomId == classroom);
        }

        return [.. (await (from entry in this.context.AttendanceEntries.AsNoTracking()
                           join sheet in sheets on entry.SheetId equals sheet.Id
                           where entry.Status != AttendanceStatus.Present
                           select new { entry.StudentId, sheet.Day, entry.Status })
                .Distinct()
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(x => (x.StudentId, x.Day, x.Status))];
    }

    public void AddSheet(AttendanceSheet sheet) => this.context.AttendanceSheets.Add(sheet);

    public void AddEntry(AttendanceEntry entry) => this.context.AttendanceEntries.Add(entry);
}
