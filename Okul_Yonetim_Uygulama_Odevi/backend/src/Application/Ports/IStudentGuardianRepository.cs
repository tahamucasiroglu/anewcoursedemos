using OkulYonetim.Application.Features.Students;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Veli-öğrenci bağları (spec 0009). Okumalar kurum filtresinden geçer; öğrenci ve şube adları da kendi filtrelerinden
/// geçerek eklenir. Velinin görebildiği öğrenciler yalnızca <see cref="ListChildrenAsync"/> / <see cref="IsLinkedAsync"/>
/// üzerinden belirlenir (ilişki duvarı, plan Risks 4).
/// </summary>
public interface IStudentGuardianRepository
{
    Task<StudentGuardian?> FindAsync(Guid studentId, Guid guardianUserId, CancellationToken cancellationToken);

    Task<int> CountForStudentAsync(Guid studentId, CancellationToken cancellationToken);

    /// <summary>Öğrencinin velileri (kişi bilgisiyle), bağlanma sırasıyla.</summary>
    Task<IReadOnlyList<StudentGuardianView>> ListGuardiansAsync(Guid studentId, CancellationToken cancellationToken);

    /// <summary>Aynı veliye bağlı diğer öğrenciler (kardeşler), ad sırası uygulamada.</summary>
    Task<IReadOnlyList<SiblingRow>> ListSiblingsAsync(Guid studentId, CancellationToken cancellationToken);

    /// <summary>Velinin bağlı ve "ayrıldı" olmayan çocukları (seçili okulda).</summary>
    Task<IReadOnlyList<ChildRow>> ListChildrenAsync(Guid guardianUserId, CancellationToken cancellationToken);

    Task<bool> IsLinkedAsync(Guid studentId, Guid guardianUserId, CancellationToken cancellationToken);

    void Add(StudentGuardian link);

    void Remove(StudentGuardian link);
}
