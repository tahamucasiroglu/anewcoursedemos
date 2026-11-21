using OkulYonetim.Application.Features.Students;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Öğrenciler ve teslim alabilecek kişileri (spec 0009). Her okuma kurum + şube filtresinden geçer: öğrenci, şubesinin
/// görünürlüğünü miras alır (kurucu okulun bütün öğrencilerini görür). Görünmeyen öğrenci "yok" sayılır (S1). Velinin
/// "kendi çocuğu" duvarı burada değil, <see cref="IStudentGuardianRepository"/> bağlarıyla uygulanır.
/// </summary>
public interface IStudentRepository
{
    Task<Student?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Kurucu listesi için bütün (görünür) öğrencilerin satırları — sıralama, arama ve sayfalama uygulamada.</summary>
    Task<IReadOnlyList<StudentRow>> ListRowsAsync(CancellationToken cancellationToken);

    /// <summary>Seçili okulda aynı okul numarası (karşılaştırma biçimiyle) başka bir öğrencide mi; <paramref name="exceptId"/> hariç.</summary>
    Task<bool> SchoolNumberExistsAsync(string schoolNumberKey, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>Sınıfa kayıtlı, "ayrıldı" olmayan (görünür) öğrenciler — yoklama listesi (spec 0010); sıralama uygulamada.</summary>
    Task<IReadOnlyList<RosterStudent>> ListClassroomRosterAsync(Guid classroomId, CancellationToken cancellationToken);

    /// <summary>Görünür, ayrılmamış öğrencilerin kısa bilgisi; verilen şube/sınıf/kimliklerle süzülür (spec 0011).</summary>
    Task<IReadOnlyList<StudentBrief>> ListBriefsAsync(Guid? branchId, Guid? classroomId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken);

    /// <summary>Öğrenci etiketleri (spec 0015): ayrılmışlar dahil, filtreli; şube ve sınıf adıyla.</summary>
    Task<IReadOnlyList<StudentLabel>> ListLabelsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyList<PickupPerson>> ListPickupPersonsAsync(Guid studentId, CancellationToken cancellationToken);

    void Add(Student student);

    void AddPickupPerson(PickupPerson person);

    void RemovePickupPerson(PickupPerson person);
}
