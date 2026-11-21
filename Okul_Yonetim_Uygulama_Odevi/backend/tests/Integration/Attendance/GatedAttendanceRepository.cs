using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Attendance;
using OkulYonetim.Tests.Integration.Administration;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Gerçek yoklama deposunun sarmalayıcısı (spec 0010 AC-11): kaydetmenin sıra içindeki "bugünün yoklaması var mı" okuması
/// (<see cref="FindSheetAsync"/>) <see cref="RaceGate"/>'ten geçer; diğer işlemler olduğu gibi aktarılır.
/// </summary>
public sealed class GatedAttendanceRepository : IAttendanceRepository
{
    private readonly IAttendanceRepository inner;
    private readonly RaceGate gate;

    public GatedAttendanceRepository(IAttendanceRepository inner, RaceGate gate)
    {
        this.inner = inner;
        this.gate = gate;
    }

    public async Task<AttendanceSheet?> FindSheetAsync(Guid classroomId, DateOnly day, CancellationToken cancellationToken)
    {
        await this.gate.PassAsync().ConfigureAwait(false);
        return await this.inner.FindSheetAsync(classroomId, day, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<AttendanceEntry>> ListEntriesAsync(Guid sheetId, CancellationToken cancellationToken) => this.inner.ListEntriesAsync(sheetId, cancellationToken);

    public Task<IReadOnlyList<Guid>> ListTakenClassroomIdsAsync(IReadOnlyCollection<Guid> classroomIds, DateOnly day, CancellationToken cancellationToken) =>
        this.inner.ListTakenClassroomIdsAsync(classroomIds, day, cancellationToken);

    public Task<IReadOnlyList<(DateOnly Day, AttendanceStatus Status)>> ListStudentDaysAsync(Guid studentId, DateOnly fromDay, DateOnly toDay, CancellationToken cancellationToken) =>
        this.inner.ListStudentDaysAsync(studentId, fromDay, toDay, cancellationToken);

    public void AddSheet(AttendanceSheet sheet) => this.inner.AddSheet(sheet);

    public void AddEntry(AttendanceEntry entry) => this.inner.AddEntry(entry);
}
