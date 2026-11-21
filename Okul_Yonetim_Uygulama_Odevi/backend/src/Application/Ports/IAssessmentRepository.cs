using OkulYonetim.Application.Features.Academics.Assessments;
using OkulYonetim.Application.Features.Academics.Records;
using OkulYonetim.Domain.Academics;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Değerlendirmeler ve puanlar (spec 0013). Değerlendirme kurum + şube filtresinden geçer; puan satırları kurum filtreli.
/// Değerlendirme + öğrenci tekildir. Okumalar izlemesiz projeksiyonla (T7); kaydetme için izlenen satırlar.
/// </summary>
public interface IAssessmentRepository
{
    /// <summary>Değerlendirme ve açan öğretmenin adı; görünmüyorsa <c>null</c>.</summary>
    Task<(Assessment Assessment, string CreatorName)?> FindWithCreatorAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Sınıfın değerlendirmeleri güne göre yeniden eskiye; sayılar yalnızca verilen (sınıfta süren) öğrencilerle.</summary>
    Task<IReadOnlyList<AssessmentRow>> ListForClassroomAsync(Guid classroomId, IReadOnlyCollection<Guid> currentStudentIds, CancellationToken cancellationToken);

    /// <summary>Değerlendirmenin puan satırları; <paramref name="forUpdate"/> doğruysa izlenir (kaydetme için).</summary>
    Task<IReadOnlyList<Grade>> ListGradesAsync(Guid assessmentId, bool forUpdate, CancellationToken cancellationToken);

    /// <summary>Öğrencinin günü aralıkta (uçlar dahil) olan değerlendirmeleri ve kendi puanı, güne göre.</summary>
    Task<IReadOnlyList<StudentGradeRecord>> ListForStudentAsync(Guid studentId, DateOnly fromDay, DateOnly toDay, CancellationToken cancellationToken);

    void Add(Assessment assessment);

    void AddGrade(Grade grade);
}
