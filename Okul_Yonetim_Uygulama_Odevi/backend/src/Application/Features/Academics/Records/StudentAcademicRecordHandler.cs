using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Academics.Records;

/// <summary>
/// Öğrenci bazlı okuma (spec 0013 gereksinim 8): öğrencinin teslim günü aralıktaki ödevleri kendi durumuyla ve günü aralıktaki
/// notları kendi puanıyla. Yalnızca bu öğrencinin satırları okunur (aynı sınıfın başka öğrencisininki gelmez); kurum ve şube
/// filtresi uygulanır. Erişim denetimi çağırandadır (<see cref="GetStudentAcademicRecordQuery"/>). Ters aralık boş döner.
/// </summary>
public sealed class StudentAcademicRecordHandler : IRequestHandler<GetStudentAcademicRecordQuery, StudentAcademicRecord>
{
    private readonly IAssignmentRepository assignments;
    private readonly IAssessmentRepository assessments;

    public StudentAcademicRecordHandler(IAssignmentRepository assignments, IAssessmentRepository assessments)
    {
        this.assignments = assignments;
        this.assessments = assessments;
    }

    public async Task<StudentAcademicRecord> Handle(GetStudentAcademicRecordQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.From > request.To)
        {
            return new StudentAcademicRecord([], []);
        }

        return new StudentAcademicRecord(
            await this.assignments.ListForStudentAsync(request.StudentId, request.From, request.To, cancellationToken).ConfigureAwait(false),
            await this.assessments.ListForStudentAsync(request.StudentId, request.From, request.To, cancellationToken).ConfigureAwait(false));
    }
}
