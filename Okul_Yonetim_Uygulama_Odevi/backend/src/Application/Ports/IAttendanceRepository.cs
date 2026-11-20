using OkulYonetim.Domain.Attendance;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Yoklamalar (spec 0010). Yoklama kurum + şube filtresinden geçer (görünmeyen şubenin yoklaması "yok"); satırlar kurum
/// filtreli. Sınıf + gün tekildir.
/// </summary>
public interface IAttendanceRepository
{
    Task<AttendanceSheet?> FindSheetAsync(Guid classroomId, DateOnly day, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceEntry>> ListEntriesAsync(Guid sheetId, CancellationToken cancellationToken);

    /// <summary>Verilen sınıflardan o gün yoklaması alınmış olanlar.</summary>
    Task<IReadOnlyList<Guid>> ListTakenClassroomIdsAsync(IReadOnlyCollection<Guid> classroomIds, DateOnly day, CancellationToken cancellationToken);

    void AddSheet(AttendanceSheet sheet);

    void AddEntry(AttendanceEntry entry);
}
