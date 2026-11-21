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

    /// <summary>Verilen sınıfların o günkü yoklamalarında "Var" olmayan satırlar (spec 0017 devamsızlık kartı): sınıf, öğrenci, durum.</summary>
    Task<IReadOnlyList<(Guid ClassroomId, Guid StudentId, AttendanceStatus Status)>> ListDayExceptionsAsync(IReadOnlyCollection<Guid> classroomIds, DateOnly day, CancellationToken cancellationToken);

    /// <summary>
    /// Günü aralıkta (uçlar dahil) olan yoklamalardaki "Yok" ve "Geç" satırları, öğrenci + gün + durum başına bir kez (spec 0017 devamsızlık
    /// raporu); şube ve sınıf yoklamanın alındığı şube ve sınıftır. Yoklama kurum + şube filtresinden geçer; izlemesiz projeksiyon.
    /// </summary>
    Task<IReadOnlyList<(Guid StudentId, DateOnly Day, AttendanceStatus Status)>> ListStatusDaysAsync(DateOnly fromDay, DateOnly toDay, Guid? branchId, Guid? classroomId, CancellationToken cancellationToken);

    void AddSheet(AttendanceSheet sheet);

    void AddEntry(AttendanceEntry entry);
}
