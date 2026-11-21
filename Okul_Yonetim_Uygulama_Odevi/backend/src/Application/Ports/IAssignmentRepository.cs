using OkulYonetim.Application.Features.Academics.Assignments;
using OkulYonetim.Application.Features.Academics.Records;
using OkulYonetim.Domain.Academics;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Ödevler ve teslim durumları (spec 0013). Ödev kurum + şube filtresinden geçer (görünmeyen şubenin ödevi "yok"); teslim
/// satırları kurum filtreli. Ödev + öğrenci tekildir. Okumalar izlemesiz projeksiyonla (T7); kaydetme için izlenen satırlar.
/// </summary>
public interface IAssignmentRepository
{
    /// <summary>Ödev ve veren öğretmenin adı; görünmüyorsa <c>null</c>.</summary>
    Task<(Assignment Assignment, string CreatorName)?> FindWithCreatorAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Sınıfın ödevleri teslim gününe göre yeniden eskiye; sayılar yalnızca verilen (sınıfta süren) öğrencilerle.</summary>
    Task<IReadOnlyList<AssignmentRow>> ListForClassroomAsync(Guid classroomId, IReadOnlyCollection<Guid> currentStudentIds, CancellationToken cancellationToken);

    /// <summary>Ödevin teslim satırları; <paramref name="forUpdate"/> doğruysa izlenir (kaydetme için).</summary>
    Task<IReadOnlyList<AssignmentSubmission>> ListSubmissionsAsync(Guid assignmentId, bool forUpdate, CancellationToken cancellationToken);

    /// <summary>Öğrencinin teslim günü aralıkta (uçlar dahil) olan ödevleri ve kendi durumu, teslim gününe göre.</summary>
    Task<IReadOnlyList<StudentAssignmentRecord>> ListForStudentAsync(Guid studentId, DateOnly fromDay, DateOnly toDay, CancellationToken cancellationToken);

    void Add(Assignment assignment);

    void AddSubmission(AssignmentSubmission submission);
}
