using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Communication;
using OkulYonetim.Application.Features.Communication.Notifications;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListClassroomRoster;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Academics;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>
/// Ödev ve teslim durumu (spec 0013 gereksinim 1–4). Yetki her istekte <see cref="AcademicsGuard"/> ile: öğretmen değilse 403,
/// sınıfa ya da kaydın sınıfına erişim yoksa 404, teslim durumunu yalnızca ödevi veren değiştirir (403).
/// <para>
/// Verme: ödevin öğrencileri o anki sınıf listesidir (Students'ın <c>ListClassroomRosterQuery</c>'si), hepsi "İşaretlenmedi";
/// ödev, teslim satırları, veli bildirimleri (Communication'ın <see cref="NotifyStudentGuardiansCommand"/>'ı) ve audit tek
/// işlemde yazılır; bildirimler ancak commit'ten sonra bildirim kapısından çıkar (<see cref="DeliverNotificationsCommand"/> —
/// gönderici hatası ödevi geri almaz). Liste ve teslim listesi yalnızca sınıfta süren öğrencileri gösterir ve sayar (sınıftan
/// ayrılanın satırı kalır, velinin özetinde görünür). Kaydetme ödev başına sıradadır (<c>assignment:&lt;ödev&gt;</c>): öğrenci başına
/// son kayıt geçerli. Loglarda ve audit'te yalnızca kimlik ve sayı; ödev metni ve öğrenci adı yok.
/// </para>
/// </summary>
public sealed class AssignmentHandlers :
    IRequestHandler<CreateAssignmentCommand, AssignmentCreated>,
    IRequestHandler<ListClassroomAssignmentsQuery, IReadOnlyList<AssignmentRow>>,
    IRequestHandler<GetAssignmentSubmissionsQuery, AssignmentSubmissionsView>,
    IRequestHandler<SaveSubmissionsCommand, SubmissionsSaveResult>
{
    private readonly AcademicsGuard guard;
    private readonly IAssignmentRepository assignments;
    private readonly SchoolCalendar calendar;
    private readonly ISender sender;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<AssignmentHandlers> logger;

    public AssignmentHandlers(
        AcademicsGuard guard,
        IAssignmentRepository assignments,
        SchoolCalendar calendar,
        ISender sender,
        AdminAudit audit,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<AssignmentHandlers> logger)
    {
        this.guard = guard;
        this.assignments = assignments;
        this.calendar = calendar;
        this.sender = sender;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<AssignmentCreated> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        ClassroomSummary classroom = await this.guard.RequireClassroomAsync(userId, branchIds, request.ClassroomId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(classroom.Id), cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        DateOnly today = this.calendar.Today();
        Assignment assignment = Assignment.Create(tenantId, classroom.BranchId, classroom.Id, userId, request.Subject!, request.Title!, request.Description, request.DueDate!.Value, today, now);
        this.assignments.Add(assignment);
        foreach (RosterStudent student in roster)
        {
            this.assignments.AddSubmission(AssignmentSubmission.Start(assignment, student.Id, now));
        }

        string text = AssignmentNoticeText.Render(
            await this.localization.AcademicsTextAsync(AcademicsMessages.NewAssignmentNotice).ConfigureAwait(false),
            assignment.Subject,
            assignment.Title,
            assignment.DueDate);
        SchoolNoticeResult notices = await this.sender.Send(
            new NotifyStudentGuardiansCommand([.. roster.Select(s => s.Id)], GuardianNoticeKind.Assignment, assignment.Id, today, text),
            cancellationToken).ConfigureAwait(false);
        this.audit.Record(
            AuditAction.AssignmentCreated,
            tenantId,
            userId,
            nameof(Assignment),
            assignment.Id,
            string.Create(CultureInfo.InvariantCulture, $"classroom={classroom.Id:D};students={roster.Count}"));
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Kayıt kalıcı olduktan sonra Communication'ın bildirim kapısına: veliye giden her şey tek çıkıştan geçer (spec 0011).
        await this.sender.Send(new DeliverNotificationsCommand(notices.Pushes, []), cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation(
            "Ödev verildi: sınıf {ClassroomId}, ödev {AssignmentId}, öğrenci {StudentCount}, bildirim giden veli {Notified}",
            classroom.Id,
            assignment.Id,
            roster.Count,
            notices.NotifiedGuardians);
        return new AssignmentCreated(assignment.Id, roster.Count, notices.NotifiedGuardians);
    }

    public async Task<IReadOnlyList<AssignmentRow>> Handle(ListClassroomAssignmentsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (_, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        ClassroomSummary classroom = await this.guard.RequireClassroomAsync(userId, branchIds, request.ClassroomId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(classroom.Id), cancellationToken).ConfigureAwait(false);
        return await this.assignments.ListForClassroomAsync(classroom.Id, [.. roster.Select(s => s.Id)], cancellationToken).ConfigureAwait(false);
    }

    public async Task<AssignmentSubmissionsView> Handle(GetAssignmentSubmissionsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid userId, Assignment assignment, string creatorName) = await this.RequireAssignmentAsync(request.AssignmentId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(assignment.ClassroomId), cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, AssignmentSubmission> submissions = (await this.assignments.ListSubmissionsAsync(assignment.Id, false, cancellationToken).ConfigureAwait(false))
            .ToDictionary(s => s.StudentId);
        return new AssignmentSubmissionsView(
            assignment.Id,
            assignment.Subject,
            assignment.Title,
            assignment.DueDate,
            creatorName,
            assignment.CreatedByUserId == userId,
            [.. roster.Where(s => submissions.ContainsKey(s.Id)).Select(s => new SubmissionStudentRow(s.Id, s.FullName, submissions[s.Id].Status.ToString()))]);
    }

    public async Task<SubmissionsSaveResult> Handle(SaveSubmissionsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid userId, Assignment assignment, _) = await this.RequireAssignmentAsync(request.AssignmentId, cancellationToken).ConfigureAwait(false);
        await this.guard.EnsureOwnerAsync(assignment.CreatedByUserId, userId).ConfigureAwait(false);

        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync("assignment:" + assignment.Id.ToString("N"), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.AcademicsTextAsync(AcademicsMessages.TooManyRequests).ConfigureAwait(false));
        }

        await using (scope.ConfigureAwait(false))
        {
            Dictionary<Guid, AssignmentSubmission> submissions = (await this.assignments.ListSubmissionsAsync(assignment.Id, true, cancellationToken).ConfigureAwait(false))
                .ToDictionary(s => s.StudentId);
            if (request.Entries!.Any(e => !submissions.ContainsKey(e.StudentId)))
            {
                throw new BusinessException(await this.localization.AcademicsTextAsync(AcademicsMessages.StudentNotInRecord).ConfigureAwait(false));
            }

            HashSet<Guid> current = [.. (await this.sender.Send(new ListClassroomRosterQuery(assignment.ClassroomId), cancellationToken).ConfigureAwait(false)).Select(s => s.Id)];
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            int changed = 0;
            foreach (SubmissionInput entry in request.Entries!.Where(e => current.Contains(e.StudentId)))
            {
                _ = SaveSubmissionsCommandValidator.TryParseStatus(entry.Status, out SubmissionStatus status);
                if (submissions[entry.StudentId].Set(status, userId, now))
                {
                    changed++;
                }
            }

            List<AssignmentSubmission> visible = [.. submissions.Values.Where(s => current.Contains(s.StudentId))];
            this.audit.Record(
                AuditAction.SubmissionsRecorded,
                assignment.TenantId,
                userId,
                nameof(Assignment),
                assignment.Id,
                string.Create(CultureInfo.InvariantCulture, $"classroom={assignment.ClassroomId:D};changed={changed}"));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation(
                "Teslim durumu kaydedildi: sınıf {ClassroomId}, ödev {AssignmentId}, değişen {Changed}",
                assignment.ClassroomId,
                assignment.Id,
                changed);
            return new SubmissionsSaveResult(
                visible.Count(s => s.Status == SubmissionStatus.Submitted),
                visible.Count(s => s.Status == SubmissionStatus.NotSubmitted),
                visible.Count(s => s.Status == SubmissionStatus.Unmarked),
                changed);
        }
    }

    /// <summary>Öğretmen + görünür ödev + ödevin sınıfına erişim; değilse 403 / 404.</summary>
    private async Task<(Guid UserId, Assignment Assignment, string CreatorName)> RequireAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        (_, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        (Assignment Assignment, string CreatorName) found = await this.assignments.FindWithCreatorAsync(assignmentId, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(AcademicsMessages.AssignmentNotFound).ConfigureAwait(false);
        await this.guard.RequireRecordClassroomAsync(userId, branchIds, found.Assignment.ClassroomId, AcademicsMessages.AssignmentNotFound, cancellationToken).ConfigureAwait(false);
        return (userId, found.Assignment, found.CreatorName);
    }
}
