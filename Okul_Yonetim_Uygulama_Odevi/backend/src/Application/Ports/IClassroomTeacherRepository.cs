using OkulYonetim.Application.Features.Administration.ClassroomTeachers;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Sınıf öğretmeni atamaları (spec 0010). Okumalar kurum + şube filtresinden geçer (atama, sınıf gibi şubesinin
/// görünürlüğünü miras alır). Aday öğretmenler: seçili okulda etkin üyeliği ve verilen şubede Öğretmen rolü olan kişiler.
/// </summary>
public interface IClassroomTeacherRepository
{
    Task<IReadOnlyList<ClassroomTeacher>> ListForClassroomAsync(Guid classroomId, CancellationToken cancellationToken);

    /// <summary>Sınıfa atanmış kişiler (ad ve e-postayla) — rolü sonradan alınmış olsa da listelenir (kurucu kaldırabilsin).</summary>
    Task<IReadOnlyList<TeacherOption>> ListAssignedAsync(Guid classroomId, CancellationToken cancellationToken);

    /// <summary>Şubede etkin Öğretmen olan kişiler (seçili okulda etkin üyelik + o şubede Öğretmen rolü).</summary>
    Task<IReadOnlyList<TeacherOption>> ListCandidatesAsync(Guid branchId, CancellationToken cancellationToken);

    /// <summary>Kişinin atandığı, görünür sınıflar (şube ve dönem bitişiyle).</summary>
    Task<IReadOnlyList<TeacherClassroomRow>> ListForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken);

    Task<bool> IsAssignedAsync(Guid classroomId, Guid teacherUserId, CancellationToken cancellationToken);

    void Add(ClassroomTeacher assignment);

    void Remove(ClassroomTeacher assignment);
}
