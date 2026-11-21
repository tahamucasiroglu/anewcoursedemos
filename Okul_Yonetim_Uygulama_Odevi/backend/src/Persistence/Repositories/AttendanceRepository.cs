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
        // 0010 kuralı (ListStudentDaysAsync): öğrenci-gün başına son güncellenen yoklama esas. Yalnızca en az bir "Var" olmayan kaydı
        // olan öğrenci-günler okunur (kazanan "Var" ise zaten sayılmaz); şube/sınıf süzgeci kazanan kaydın sınıfına uygulanır (plan 0017 A-4).
        IQueryable<AttendanceSheet> sheets = this.context.AttendanceSheets.AsNoTracking().Where(s => s.Day >= fromDay && s.Day <= toDay);
        var marked = from other in this.context.AttendanceEntries.AsNoTracking()
                     join otherSheet in sheets on other.SheetId equals otherSheet.Id
                     where other.Status != AttendanceStatus.Present
                     select new { other.StudentId, otherSheet.Day };
        var rows = await (from entry in this.context.AttendanceEntries.AsNoTracking()
                          join sheet in sheets on entry.SheetId equals sheet.Id
                          where marked.Any(m => m.StudentId == entry.StudentId && m.Day == sheet.Day)
                          select new { entry.StudentId, sheet.Day, SheetId = sheet.Id, sheet.UpdatedAt, sheet.BranchId, sheet.ClassroomId, entry.Status })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows
            .GroupBy(r => (r.StudentId, r.Day))
            .Select(g => g.OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.SheetId).First())
            .Where(r => r.Status != AttendanceStatus.Present
                && (branchId is null || r.BranchId == branchId)
                && (classroomId is null || r.ClassroomId == classroomId))
            .Select(r => (r.StudentId, r.Day, r.Status))];
    }

    public void AddSheet(AttendanceSheet sheet) => this.context.AttendanceSheets.Add(sheet);

    public void AddEntry(AttendanceEntry entry) => this.context.AttendanceEntries.Add(entry);
}
