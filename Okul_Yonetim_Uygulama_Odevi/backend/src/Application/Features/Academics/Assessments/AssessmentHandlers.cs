using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetTermById;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListClassroomRoster;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Academics;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>
/// Değerlendirme ve puan (spec 0013 gereksinim 5–6). Yetki <see cref="AcademicsGuard"/> ile (öğretmen değilse 403, sınıfa ya da
/// kaydın sınıfına erişim yoksa 404, puanı yalnızca açan değiştirir — 403).
/// <para>
/// Açma: gün sınıfın döneminin içinde olmalı (Organization'ın <see cref="GetTermByIdQuery"/>'si; değilse <c>Date</c> alanında
/// doğrulama hatası); değerlendirmenin öğrencileri o anki sınıf listesidir, hepsi "girilmedi". Puan kaydı değerlendirme başına
/// sıradadır (<c>assessment:&lt;değerlendirme&gt;</c>); doğrulama bütün puanları istekten önce denetler (biri geçersizse hiçbiri
/// yazılmaz), veritabanında da <c>CHECK 0–100</c> vardır. Liste ve puan listesi yalnızca sınıfta süren öğrencileri gösterir ve
/// sayar. Loglarda ve audit'te yalnızca kimlik ve değişen sayısı; puan değeri ve öğrenci adı yok.
/// </para>
/// </summary>
public sealed class AssessmentHandlers :
    IRequestHandler<CreateAssessmentCommand, AssessmentCreated>,
    IRequestHandler<ListClassroomAssessmentsQuery, IReadOnlyList<AssessmentRow>>,
    IRequestHandler<GetAssessmentGradesQuery, AssessmentGradesView>,
    IRequestHandler<SaveGradesCommand, GradesSaveResult>
{
    private readonly AcademicsGuard guard;
    private readonly IAssessmentRepository assessments;
    private readonly SchoolCalendar calendar;
    private readonly ISender sender;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<AssessmentHandlers> logger;

    public AssessmentHandlers(
        AcademicsGuard guard,
        IAssessmentRepository assessments,
        SchoolCalendar calendar,
        ISender sender,
        AdminAudit audit,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<AssessmentHandlers> logger)
    {
        this.guard = guard;
        this.assessments = assessments;
        this.calendar = calendar;
        this.sender = sender;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<AssessmentCreated> Handle(CreateAssessmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        ClassroomSummary classroom = await this.guard.RequireClassroomAsync(userId, branchIds, request.ClassroomId, cancellationToken).ConfigureAwait(false);
        TermSummary term = await this.sender.Send(new GetTermByIdQuery(classroom.TermId), cancellationToken).ConfigureAwait(false);
        if (!term.Contains(request.Date!.Value))
        {
            string message = await this.localization.AcademicsTextAsync(AcademicsMessages.DateOutsideTerm).ConfigureAwait(false);
            throw new ValidationException([new ValidationExceptionModel { Property = nameof(CreateAssessmentCommand.Date), Errors = [message] }]);
        }

        _ = CreateAssessmentCommandValidator.TryParseKind(request.Kind, out AssessmentKind kind);
        IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(classroom.Id), cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        Assessment assessment = Assessment.Create(tenantId, classroom.BranchId, classroom.Id, userId, request.Subject!, kind, request.Title!, request.Date.Value, this.calendar.Today(), now);
        this.assessments.Add(assessment);
        foreach (RosterStudent student in roster)
        {
            this.assessments.AddGrade(Grade.Start(assessment, student.Id, now));
        }

        this.audit.Record(
            AuditAction.AssessmentCreated,
            tenantId,
            userId,
            nameof(Assessment),
            assessment.Id,
            string.Create(CultureInfo.InvariantCulture, $"classroom={classroom.Id:D};students={roster.Count}"));
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation(
            "Değerlendirme açıldı: sınıf {ClassroomId}, değerlendirme {AssessmentId}, öğrenci {StudentCount}",
            classroom.Id,
            assessment.Id,
            roster.Count);
        return new AssessmentCreated(assessment.Id, roster.Count);
    }

    public async Task<IReadOnlyList<AssessmentRow>> Handle(ListClassroomAssessmentsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (_, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        ClassroomSummary classroom = await this.guard.RequireClassroomAsync(userId, branchIds, request.ClassroomId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(classroom.Id), cancellationToken).ConfigureAwait(false);
        return await this.assessments.ListForClassroomAsync(classroom.Id, [.. roster.Select(s => s.Id)], cancellationToken).ConfigureAwait(false);
    }

    public async Task<AssessmentGradesView> Handle(GetAssessmentGradesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid userId, Assessment assessment, string creatorName) = await this.RequireAssessmentAsync(request.AssessmentId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(assessment.ClassroomId), cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, Grade> grades = (await this.assessments.ListGradesAsync(assessment.Id, false, cancellationToken).ConfigureAwait(false))
            .ToDictionary(g => g.StudentId);
        return new AssessmentGradesView(
            assessment.Id,
            assessment.Subject,
            assessment.Kind.ToString(),
            assessment.Title,
            assessment.Date,
            creatorName,
            assessment.CreatedByUserId == userId,
            [.. roster.Where(s => grades.ContainsKey(s.Id)).Select(s => new GradeStudentRow(s.Id, s.FullName, grades[s.Id].Score))]);
    }

    public async Task<GradesSaveResult> Handle(SaveGradesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid userId, Assessment assessment, _) = await this.RequireAssessmentAsync(request.AssessmentId, cancellationToken).ConfigureAwait(false);
        await this.guard.EnsureOwnerAsync(assessment.CreatedByUserId, userId).ConfigureAwait(false);

        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync("assessment:" + assessment.Id.ToString("N"), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.AcademicsTextAsync(AcademicsMessages.TooManyRequests).ConfigureAwait(false));
        }

        await using (scope.ConfigureAwait(false))
        {
            Dictionary<Guid, Grade> grades = (await this.assessments.ListGradesAsync(assessment.Id, true, cancellationToken).ConfigureAwait(false))
                .ToDictionary(g => g.StudentId);
            if (request.Grades!.Any(g => !grades.ContainsKey(g.StudentId)))
            {
                throw new BusinessException(await this.localization.AcademicsTextAsync(AcademicsMessages.StudentNotInRecord).ConfigureAwait(false));
            }

            HashSet<Guid> current = [.. (await this.sender.Send(new ListClassroomRosterQuery(assessment.ClassroomId), cancellationToken).ConfigureAwait(false)).Select(s => s.Id)];
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            int changed = 0;
            foreach (GradeInput input in request.Grades!.Where(g => current.Contains(g.StudentId)))
            {
                _ = SaveGradesCommandValidator.TryParseScore(input.Score, out int? score);
                if (grades[input.StudentId].SetScore(score, userId, now))
                {
                    changed++;
                }
            }

            List<Grade> visible = [.. grades.Values.Where(g => current.Contains(g.StudentId))];
            this.audit.Record(
                AuditAction.GradesRecorded,
                assessment.TenantId,
                userId,
                nameof(Assessment),
                assessment.Id,
                string.Create(CultureInfo.InvariantCulture, $"classroom={assessment.ClassroomId:D};changed={changed}"));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation(
                "Puanlar kaydedildi: sınıf {ClassroomId}, değerlendirme {AssessmentId}, değişen {Changed}",
                assessment.ClassroomId,
                assessment.Id,
                changed);
            return new GradesSaveResult(visible.Count(g => g.Score is not null), visible.Count, changed);
        }
    }

    /// <summary>Öğretmen + görünür değerlendirme + sınıfına erişim; değilse 403 / 404.</summary>
    private async Task<(Guid UserId, Assessment Assessment, string CreatorName)> RequireAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        (_, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        (Assessment Assessment, string CreatorName) found = await this.assessments.FindWithCreatorAsync(assessmentId, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(AcademicsMessages.AssessmentNotFound).ConfigureAwait(false);
        await this.guard.RequireRecordClassroomAsync(userId, branchIds, found.Assessment.ClassroomId, AcademicsMessages.AssessmentNotFound, cancellationToken).ConfigureAwait(false);
        return (userId, found.Assessment, found.CreatorName);
    }
}
