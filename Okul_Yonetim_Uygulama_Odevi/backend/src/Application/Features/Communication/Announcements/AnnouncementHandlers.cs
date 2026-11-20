using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Communication.Notifications;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetBranchById;
using OkulYonetim.Application.Features.Organization.Queries.GetClassroomById;
using OkulYonetim.Application.Features.Organization.Queries.ListClassrooms;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListGuardianContacts;
using OkulYonetim.Application.Features.Students.Queries.ListStudentBriefs;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>
/// Duyuru, okundu bilgisi ve onay formu (spec 0011 gereksinim 1–4, 7). Yetki her istekte <see cref="CommunicationGuard"/>
/// ile: yayınlayan kurucu (okul, şube, sınıf, öğrenci) ya da öğretmen (yalnızca atandığı etkin sınıflar ve o sınıfların
/// öğrencileri — okul/şube kapsamı 403, başka sınıf/öğrenci 404); veli ve muhasebe 403.
/// <para>
/// Alıcılar yayın anında belirlenir: kapsamdaki görünür ve ayrılmamış öğrencilerin okulda etkin velileri (veli başına tek
/// alıcı satırı ve tek bildirim); kapsanan öğrenci = en az bir alıcı velisi olan öğrenci. Hiç veli yoksa reddedilir.
/// Bildirim kayıttan sonra bildirim kapısından (<see cref="GuardianNotifier"/>) çıkar.
/// </para>
/// <para>
/// Veli yalnızca alıcısı olduğu duyuruyu görür (başkası 404); ayrıntıyı ilk açışı okundu yazar. Sonuçları (okundu X / Y,
/// okumayanlar, onay) yalnızca yayınlayan ve kurucu görür. Onay cevabı öğrenci başınadır: kapsanan ve velinin bağlı olduğu
/// çocuk için, son gün (Türkiye günü) dahil; duyuru + öğrenci sırasında (<c>consent:&lt;duyuru&gt;:&lt;öğrenci&gt;</c>) yazılır,
/// son cevap geçerlidir. Yayın ve her cevap audit'li; loglarda yalnızca kimlik ve sayı (metin ve ad yok).
/// </para>
/// </summary>
public sealed class AnnouncementHandlers :
    IRequestHandler<PublishAnnouncementCommand, AnnouncementPublished>,
    IRequestHandler<ListStaffAnnouncementsQuery, IReadOnlyList<StaffAnnouncementRow>>,
    IRequestHandler<ListMyFeedQuery, FeedPage>,
    IRequestHandler<GetAnnouncementQuery, AnnouncementDetail>,
    IRequestHandler<GetAnnouncementResultsQuery, AnnouncementResults>,
    IRequestHandler<AnswerConsentCommand, ConsentChildView>,
    IRequestHandler<ListScopeClassroomsQuery, IReadOnlyList<ScopeClassroom>>,
    IRequestHandler<ListScopeStudentsQuery, IReadOnlyList<ScopeStudent>>
{
    public const int PageSize = 20;
    public const int MaxStaffListed = 100;

    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    private readonly CommunicationGuard guard;
    private readonly IAnnouncementRepository announcements;
    private readonly ISender sender;
    private readonly SchoolCalendar calendar;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly GuardianNotifier notifier;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<AnnouncementHandlers> logger;

    public AnnouncementHandlers(
        CommunicationGuard guard,
        IAnnouncementRepository announcements,
        ISender sender,
        SchoolCalendar calendar,
        AdminAudit audit,
        IUnitOfWork unitOfWork,
        GuardianNotifier notifier,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<AnnouncementHandlers> logger)
    {
        this.guard = guard;
        this.announcements = announcements;
        this.sender = sender;
        this.calendar = calendar;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.notifier = notifier;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<AnnouncementPublished> Handle(PublishAnnouncementCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (!caller.IsFounder && !caller.IsTeacher)
        {
            throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
        }

        AnnouncementScope scope = Enum.Parse<AnnouncementScope>(request.Scope!);
        if (!caller.IsFounder && scope is AnnouncementScope.School or AnnouncementScope.Branch)
        {
            throw new ForbiddenException(await this.localization.CommunicationTextAsync(CommunicationMessages.TeacherScopeOnly).ConfigureAwait(false));
        }

        IReadOnlyList<StudentBrief> students = await this.ScopeStudentsAsync(caller, scope, request.ScopeId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<GuardianContact> contacts = students.Count == 0
            ? []
            : await this.sender.Send(new ListGuardianContactsQuery([.. students.Select(s => s.Id)]), cancellationToken).ConfigureAwait(false);
        if (contacts.Count == 0)
        {
            throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.NoRecipients).ConfigureAwait(false));
        }

        DateTimeOffset now = this.timeProvider.GetUtcNow();
        Announcement announcement = Announcement.Publish(
            caller.TenantId,
            caller.UserId,
            scope,
            request.ScopeId,
            request.Title!,
            request.Body!,
            request.RequestConsent ? request.ConsentQuestion : null,
            request.RequestConsent ? request.ConsentDeadline : null,
            now);
        this.announcements.Add(announcement);
        List<Guid> coveredStudents = [.. contacts.Select(c => c.StudentId).Distinct()];
        List<Guid> guardians = [.. contacts.Select(c => c.UserId).Distinct()];
        foreach (Guid studentId in coveredStudents)
        {
            this.announcements.AddStudent(AnnouncementStudent.For(announcement, studentId));
        }

        foreach (Guid guardianId in guardians)
        {
            this.announcements.AddRecipient(AnnouncementRecipient.For(announcement, guardianId));
        }

        this.audit.Record(
            AuditAction.AnnouncementPublished,
            caller.TenantId,
            caller.UserId,
            nameof(Announcement),
            announcement.Id,
            string.Create(CultureInfo.InvariantCulture, $"scope={scope};consent={announcement.RequiresConsent};recipients={guardians.Count};students={coveredStudents.Count}"));
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation(
            "Duyuru yayınlandı: duyuru {AnnouncementId}, yayınlayan {UserId}, kapsam {Scope}, alıcı veli {RecipientCount}, öğrenci {StudentCount}",
            announcement.Id,
            caller.UserId,
            scope,
            guardians.Count,
            coveredStudents.Count);

        PushNotificationKind kind = announcement.RequiresConsent ? PushNotificationKind.ConsentForm : PushNotificationKind.Announcement;
        await this.notifier.DeliverAsync([.. guardians.Select(g => new PushNotification(caller.TenantId, g, kind, announcement.Id))], [], cancellationToken).ConfigureAwait(false);
        return new AnnouncementPublished(announcement.Id, guardians.Count);
    }

    public async Task<IReadOnlyList<StaffAnnouncementRow>> Handle(ListStaffAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (!caller.IsFounder && !caller.IsTeacher)
        {
            throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
        }

        return await this.announcements.ListForStaffAsync(caller.IsFounder ? null : caller.UserId, MaxStaffListed, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FeedPage> Handle(ListMyFeedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (!caller.IsGuardian)
        {
            throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
        }

        int page = Math.Max(1, request.Page);
        int take = (page * PageSize) + 1;
        IReadOnlyList<ChildRow> children = await this.guard.ChildrenAsync(caller, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FeedAnnouncementRow> rows = await this.announcements.ListForGuardianAsync(caller.UserId, take, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<NoticeView> notices = children.Count == 0
            ? []
            : await this.sender.Send(new ListMyNoticesQuery(take), cancellationToken).ConfigureAwait(false);

        List<FeedItem> merged =
        [
            .. rows.Select(a => new FeedItem("Announcement", a.Id, a.Title, a.AuthorName, [], a.PublishedAt, a.IsRead, a.RequiresConsent)),
            .. notices.Select(n => new FeedItem("SchoolNotice", n.Id, n.Text, null, [n.StudentName], n.At, n.IsRead, false)),
        ];
        List<FeedItem> items = [.. merged.OrderByDescending(i => i.At).ThenByDescending(i => i.Id).Skip((page - 1) * PageSize).Take(PageSize)];

        // Sınıf ve öğrenci kapsamında "ilgili çocuk": duyurunun kapsadığı, velinin bağlı çocukları.
        HashSet<Guid> targeted = [.. rows.Where(a => a.Scope is nameof(AnnouncementScope.Classroom) or nameof(AnnouncementScope.Student)).Select(a => a.Id)];
        List<Guid> pageAnnouncements = [.. items.Where(i => targeted.Contains(i.Id)).Select(i => i.Id)];
        if (pageAnnouncements.Count > 0 && children.Count > 0)
        {
            Dictionary<Guid, string> names = children.ToDictionary(c => c.Id, c => c.FullName);
            ILookup<Guid, string> covered = (await this.announcements.ListCoveredAsync(pageAnnouncements, names.Keys, cancellationToken).ConfigureAwait(false))
                .ToLookup(s => s.AnnouncementId, s => names[s.StudentId]);
            items = [.. items.Select(i => covered.Contains(i.Id) ? i with { ChildNames = [.. covered[i.Id].Order(TurkishOrder)] } : i)];
        }

        int unread = await this.announcements.CountUnreadAsync(caller.UserId, cancellationToken).ConfigureAwait(false);
        return new FeedPage(items, page, merged.Count > page * PageSize, unread);
    }

    public async Task<AnnouncementDetail> Handle(GetAnnouncementQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        Announcement announcement = await this.announcements.FindAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(CommunicationMessages.AnnouncementNotFound).ConfigureAwait(false);

        IReadOnlyList<ConsentChildView> children = [];
        AnnouncementRecipient? recipient = caller.IsGuardian
            ? await this.announcements.FindRecipientAsync(announcement.Id, caller.UserId, cancellationToken).ConfigureAwait(false)
            : null;
        if (recipient is not null)
        {
            if (recipient.MarkRead(this.timeProvider.GetUtcNow()))
            {
                await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                this.logger.LogInformation("Duyuru okundu: duyuru {AnnouncementId}, veli {UserId}", announcement.Id, caller.UserId);
            }

            IReadOnlyList<ChildRow> mine = await this.guard.ChildrenAsync(caller, cancellationToken).ConfigureAwait(false);
            children = mine.Count == 0
                ? []
                : [.. (await this.announcements.ListConsentAsync(announcement.Id, [.. mine.Select(c => c.Id)], cancellationToken).ConfigureAwait(false))
                    .OrderBy(c => c.StudentName, TurkishOrder)];
        }
        else if (!caller.IsFounder && announcement.AuthorUserId != caller.UserId)
        {
            throw await this.guard.NotFoundAsync(CommunicationMessages.AnnouncementNotFound).ConfigureAwait(false);
        }

        string author = await this.announcements.AuthorNameAsync(announcement.AuthorUserId, cancellationToken).ConfigureAwait(false);
        return new AnnouncementDetail(
            announcement.Id,
            announcement.Title,
            announcement.Body,
            announcement.Scope.ToString(),
            author,
            announcement.PublishedAt,
            announcement.ConsentQuestion,
            announcement.ConsentDeadline,
            announcement.AcceptsConsentOn(this.calendar.Today()),
            children);
    }

    public async Task<AnnouncementResults> Handle(GetAnnouncementResultsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (!caller.IsFounder && !caller.IsTeacher)
        {
            throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
        }

        Announcement? announcement = await this.announcements.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (announcement is null || (!caller.IsFounder && announcement.AuthorUserId != caller.UserId))
        {
            throw await this.guard.NotFoundAsync(CommunicationMessages.AnnouncementNotFound).ConfigureAwait(false);
        }

        IReadOnlyList<RecipientRow> recipients = await this.announcements.ListRecipientsAsync(announcement.Id, cancellationToken).ConfigureAwait(false);
        ILookup<Guid, string> childNames = (await this.announcements.ListRecipientChildrenAsync(announcement.Id, cancellationToken).ConfigureAwait(false))
            .ToLookup(c => c.GuardianUserId, c => c.StudentName);
        List<UnreadGuardianRow> unread = [.. recipients
            .Where(r => r.ReadAt is null)
            .OrderBy(r => r.GuardianName, TurkishOrder).ThenBy(r => r.GuardianUserId)
            .Select(r => new UnreadGuardianRow(r.GuardianName, [.. childNames[r.GuardianUserId].Order(TurkishOrder)]))];

        ConsentSummary? consent = null;
        if (announcement.RequiresConsent)
        {
            List<ConsentChildView> students = [.. (await this.announcements.ListConsentAsync(announcement.Id, null, cancellationToken).ConfigureAwait(false))
                .OrderBy(s => s.StudentName, TurkishOrder).ThenBy(s => s.StudentId)];
            consent = new ConsentSummary(
                students.Count(s => s.Answer == nameof(ConsentAnswer.Approved)),
                students.Count(s => s.Answer == nameof(ConsentAnswer.Declined)),
                students.Count(s => s.Answer is null),
                students);
        }

        return new AnnouncementResults(announcement.Id, recipients.Count, recipients.Count(r => r.ReadAt is not null), unread, consent);
    }

    public async Task<ConsentChildView> Handle(AnswerConsentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (!caller.IsGuardian)
        {
            throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
        }

        Guid studentId = request.StudentId!.Value;
        Announcement? announcement = await this.announcements.FindAsync(request.AnnouncementId, cancellationToken).ConfigureAwait(false);
        if (announcement is null || await this.announcements.FindRecipientAsync(announcement.Id, caller.UserId, cancellationToken).ConfigureAwait(false) is null)
        {
            throw await this.guard.NotFoundAsync(CommunicationMessages.AnnouncementNotFound).ConfigureAwait(false);
        }

        IReadOnlyList<ChildRow> children = await this.guard.ChildrenAsync(caller, cancellationToken).ConfigureAwait(false);
        if (!children.Any(c => c.Id == studentId) || !await this.announcements.CoversStudentAsync(announcement.Id, studentId, cancellationToken).ConfigureAwait(false))
        {
            throw await this.guard.NotFoundAsync(CommunicationMessages.StudentNotFound).ConfigureAwait(false);
        }

        if (!announcement.RequiresConsent)
        {
            throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.ConsentNotRequested).ConfigureAwait(false));
        }

        DateOnly today = this.calendar.Today();
        if (!announcement.AcceptsConsentOn(today))
        {
            throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.ConsentExpired).ConfigureAwait(false));
        }

        ConsentAnswer answer = Enum.Parse<ConsentAnswer>(request.Answer!);
        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync("consent:" + announcement.Id.ToString("N") + ":" + studentId.ToString("N"), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.TooManyRequests).ConfigureAwait(false));
        }

        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            ConsentResponse? existing = await this.announcements.FindConsentAsync(announcement.Id, studentId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                this.announcements.AddConsent(ConsentResponse.Give(announcement, studentId, answer, caller.UserId, now, today));
            }
            else
            {
                existing.Change(announcement, answer, caller.UserId, now, today);
            }

            this.audit.Record(
                AuditAction.ConsentAnswered,
                caller.TenantId,
                caller.UserId,
                nameof(Announcement),
                announcement.Id,
                string.Create(CultureInfo.InvariantCulture, $"student={studentId:D};answer={answer};changed={existing is not null}"));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        this.logger.LogInformation("Onay cevabı kaydedildi: duyuru {AnnouncementId}, öğrenci {StudentId}, veli {UserId}, cevap {Answer}", announcement.Id, studentId, caller.UserId, answer);
        return (await this.announcements.ListConsentAsync(announcement.Id, [studentId], cancellationToken).ConfigureAwait(false)).Single();
    }

    public async Task<IReadOnlyList<ScopeClassroom>> Handle(ListScopeClassroomsQuery request, CancellationToken cancellationToken)
    {
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (caller.IsFounder)
        {
            return [.. (await this.sender.Send(new ListClassroomsQuery(null), cancellationToken).ConfigureAwait(false))
                .Where(c => c.IsActive)
                .Select(c => new ScopeClassroom(c.Id, c.Name, c.BranchName))];
        }

        if (!caller.IsTeacher)
        {
            throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
        }

        return [.. (await this.guard.TeacherClassroomsAsync(caller, cancellationToken).ConfigureAwait(false))
            .OrderBy(c => c.BranchName, TurkishOrder).ThenBy(c => c.Name, TurkishOrder).ThenBy(c => c.ClassroomId)
            .Select(c => new ScopeClassroom(c.ClassroomId, c.Name, c.BranchName))];
    }

    public async Task<IReadOnlyList<ScopeStudent>> Handle(ListScopeStudentsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (!caller.IsFounder && !caller.IsTeacher)
        {
            throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
        }

        await this.RequireScopeClassroomAsync(caller, request.ClassroomId, cancellationToken).ConfigureAwait(false);
        return [.. (await this.sender.Send(new ListStudentBriefsQuery(ClassroomId: request.ClassroomId), cancellationToken).ConfigureAwait(false))
            .OrderBy(s => s.FullName, TurkishOrder).ThenBy(s => s.Id)
            .Select(s => new ScopeStudent(s.Id, s.FullName))];
    }

    /// <summary>Sınıf kapsamı: kurucuda görünür sınıf, öğretmende kapının "kendi sınıfları"ndan biri; değilse 404.</summary>
    private async Task RequireScopeClassroomAsync(CommunicationCaller caller, Guid classroomId, CancellationToken cancellationToken)
    {
        if (caller.IsFounder)
        {
            await this.sender.Send(new GetClassroomByIdQuery(classroomId), cancellationToken).ConfigureAwait(false);
        }
        else if (!(await this.guard.TeacherClassroomsAsync(caller, cancellationToken).ConfigureAwait(false)).Any(c => c.ClassroomId == classroomId))
        {
            throw await this.guard.NotFoundAsync(CommunicationMessages.ScopeNotFound).ConfigureAwait(false);
        }
    }

    /// <summary>Kapsamdaki görünür, ayrılmamış öğrenciler; kapsam görünmüyorsa ya da öğretmenin değilse 404.</summary>
    private async Task<IReadOnlyList<StudentBrief>> ScopeStudentsAsync(CommunicationCaller caller, AnnouncementScope scope, Guid? scopeId, CancellationToken cancellationToken)
    {
        switch (scope)
        {
            case AnnouncementScope.School:
                return await this.sender.Send(new ListStudentBriefsQuery(), cancellationToken).ConfigureAwait(false);
            case AnnouncementScope.Branch:
                BranchSummary branch = await this.sender.Send(new GetBranchByIdQuery(scopeId!.Value), cancellationToken).ConfigureAwait(false);
                return await this.sender.Send(new ListStudentBriefsQuery(BranchId: branch.Id), cancellationToken).ConfigureAwait(false);
            case AnnouncementScope.Classroom:
                Guid classroomId = scopeId!.Value;
                await this.RequireScopeClassroomAsync(caller, classroomId, cancellationToken).ConfigureAwait(false);
                return await this.sender.Send(new ListStudentBriefsQuery(ClassroomId: classroomId), cancellationToken).ConfigureAwait(false);
            default:
                IReadOnlyList<StudentBrief> found = await this.sender.Send(new ListStudentBriefsQuery(Ids: [scopeId!.Value]), cancellationToken).ConfigureAwait(false);
                StudentBrief? student = found.Count > 0 ? found[0] : null;
                if (student is null
                    || (!caller.IsFounder && !(await this.guard.TeacherClassroomsAsync(caller, cancellationToken).ConfigureAwait(false)).Any(c => c.ClassroomId == student.ClassroomId)))
                {
                    throw await this.guard.NotFoundAsync(CommunicationMessages.StudentNotFound).ConfigureAwait(false);
                }

                return [student];
        }
    }
}
