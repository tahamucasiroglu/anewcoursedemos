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

    /// <summary>
    /// Öğrencinin günü aralıkta (uçlar dahil) olan yoklama günleri ve durumu (spec 0014): gün başına bir satır — aynı gün iki sınıfta
    /// kayıt varsa en son güncellenen. Yoklama kurum + şube filtresinden geçer; izlemesiz projeksiyon.
    /// </summary>
    Task<IReadOnlyList<(DateOnly Day, AttendanceStatus Status)>> ListStudentDaysAsync(Guid studentId, DateOnly fromDay, DateOnly toDay, CancellationToken cancellationToken);

    void AddSheet(AttendanceSheet sheet);

    void AddEntry(AttendanceEntry entry);
}
