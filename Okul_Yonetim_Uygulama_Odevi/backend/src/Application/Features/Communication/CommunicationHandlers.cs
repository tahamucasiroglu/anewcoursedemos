using Microsoft.Extensions.Logging;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication.Announcements;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListGuardianContacts;
using OkulYonetim.Application.Features.Students.Queries.ListMyChildren;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Veliye giden iletişimin tek çıkışı (spec 0010; mimari "Communication") ve velinin bildirim ekranı.
/// <para>
/// Bildirim: değişen her öğrencinin okulda etkin velisine (bağ + etkin üyelik + Veli rolü) uygulama içi bildirim. Veli + öğrenci
/// + gün tekil: aynı gün ikinci "yok" bildirimi oluşmaz; "Yok" düzeltilince aynı bildirim "düzeltildi" olur ve yeniden
/// okunmamış sayılır; yeniden "Yok" tekrar devamsızlık bildirimi olur. Kayıt çağıranın işlemindedir; bildirim ve e-postalar
/// döndürülür ve çağıran commit'ten sonra bildirim kapısına (<c>DeliverNotificationsCommand</c>, spec 0011) verir — geri alınan
/// işlem bildirim ya da e-posta göndermez.
/// </para>
/// <para>
/// Velinin listesi, okunmamış sayısı ve "okundu" işlemi yalnızca seçili okulda veli rolündeki kişi içindir (değilse 403; sayı 0)
/// ve bildirimler yalnızca kişinin <b>bağlı</b> çocuklarınınkilerdir (Students'ın "Çocuklarım" sorgusu — bağ üzerinden, bütün
/// şubeler; plan Risks 6). Loglarda yalnızca kimlik ve sayı; ad, e-posta ve metin yok.
/// </para>
/// </summary>
public sealed class CommunicationHandlers :
    IRequestHandler<NotifyAttendanceChangesCommand, AttendanceNoticeResult>,
    IRequestHandler<ListMyNoticesQuery, IReadOnlyList<NoticeView>>,
    IRequestHandler<MarkMyNoticesReadCommand, int>,
    IRequestHandler<CountMyUnreadNoticesQuery, int>
{
    public const int MaxListed = 50;

    private readonly ICurrentSession session;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IGuardianNoticeRepository notices;
    private readonly ITenantRepository tenants;
    private readonly IUnitOfWork unitOfWork;
    private readonly ISender sender;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<CommunicationHandlers> logger;

    public CommunicationHandlers(
        ICurrentSession session,
        IRoleAssignmentRepository roleAssignments,
        IGuardianNoticeRepository notices,
        ITenantRepository tenants,
        IUnitOfWork unitOfWork,
        ISender sender,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<CommunicationHandlers> logger)
    {
        this.session = session;
        this.roleAssignments = roleAssignments;
        this.notices = notices;
        this.tenants = tenants;
        this.unitOfWork = unitOfWork;
        this.sender = sender;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<AttendanceNoticeResult> Handle(NotifyAttendanceChangesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Guid tenantId = this.session.TenantId ?? throw new InvalidOperationException("Bildirim için seçili okul gerekli.");
        HashSet<Guid> studentIds = [.. request.Changes.Select(c => c.StudentId), .. request.AbsentStudentIds];
        IReadOnlyList<GuardianContact> contacts = await this.sender.Send(new ListGuardianContactsQuery(studentIds), cancellationToken).ConfigureAwait(false);
        ILookup<Guid, GuardianContact> byStudent = contacts.ToLookup(c => c.StudentId);
        int withoutGuardian = request.AbsentStudentIds.Distinct().Count(id => !byStudent.Contains(id));
        if (request.Changes.Count == 0)
        {
            return new AttendanceNoticeResult([], [], 0, withoutGuardian);
        }

        Dictionary<(Guid Guardian, Guid Student), GuardianNotice> existing =
            (await this.notices.ListForStudentsOnDayAsync([.. request.Changes.Select(c => c.StudentId)], request.Day, cancellationToken).ConfigureAwait(false))
            .ToDictionary(n => (n.GuardianUserId, n.StudentId));
        string school = (await this.tenants.GetCurrentAsync(cancellationToken).ConfigureAwait(false))?.Name
            ?? throw new InvalidOperationException("Seçili okul okunamadı.");
        Templates templates = await this.TemplatesAsync().ConfigureAwait(false);
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        List<Ports.EmailMessage> emails = [];
        List<PushNotification> pushes = [];
        HashSet<Guid> notified = [];
        foreach (AttendanceChange change in request.Changes)
        {
            foreach (GuardianContact contact in byStudent[change.StudentId])
            {
                existing.TryGetValue((contact.UserId, change.StudentId), out GuardianNotice? notice);
                if (change.IsAbsent)
                {
                    if (notice is { Kind: GuardianNoticeKind.Absent })
                    {
                        continue;
                    }

                    if (notice is null)
                    {
                        notice = GuardianNotice.Absent(tenantId, contact.UserId, change.StudentId, request.Day, now);
                        this.notices.Add(notice);
                    }
                    else
                    {
                        notice.Change(GuardianNoticeKind.Absent, now);
                    }
                }
                else
                {
                    if (notice is not { Kind: GuardianNoticeKind.Absent })
                    {
                        continue;
                    }

                    notice.Change(GuardianNoticeKind.Corrected, now);
                }

                string text = GuardianNoticeEmail.Render(change.IsAbsent ? templates.AbsentText : templates.CorrectedText, change.StudentName, request.Day, school);
                string subject = GuardianNoticeEmail.Render(change.IsAbsent ? templates.AbsentSubject : templates.CorrectedSubject, change.StudentName, request.Day, school);
                emails.Add(GuardianNoticeEmail.Build(contact, subject, text, templates.Footer));
                pushes.Add(new PushNotification(tenantId, contact.UserId, PushNotificationKind.SchoolNotice, notice.Id));
                notified.Add(contact.UserId);
            }
        }

        this.logger.LogInformation("Veli bildirimleri hazırlandı: okul {TenantId}, gün {Day}, bildirim {NoticeCount}, veli {GuardianCount}", tenantId, request.Day, emails.Count, notified.Count);
        return new AttendanceNoticeResult(emails, pushes, notified.Count, withoutGuardian);
    }

    public async Task<IReadOnlyList<NoticeView>> Handle(ListMyNoticesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid userId, IReadOnlyList<ChildRow> children) = await this.MyChildrenAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, string> names = children.ToDictionary(c => c.Id, c => c.FullName);
        IReadOnlyList<GuardianNotice> list = await this.notices.ListForGuardianAsync(userId, names.Keys, Math.Clamp(request.Max, 1, (ListMyFeedQueryValidator.MaxPage * AnnouncementHandlers.PageSize) + 1), cancellationToken).ConfigureAwait(false);
        if (list.Count == 0)
        {
            return [];
        }

        string school = (await this.tenants.GetCurrentAsync(cancellationToken).ConfigureAwait(false))?.Name ?? string.Empty;
        Templates templates = await this.TemplatesAsync().ConfigureAwait(false);
        return [.. list.Select(n => new NoticeView(
            n.Id,
            n.StudentId,
            names[n.StudentId],
            n.Day,
            n.Kind.ToString(),
            GuardianNoticeEmail.Render(n.Kind == GuardianNoticeKind.Absent ? templates.AbsentText : templates.CorrectedText, names[n.StudentId], n.Day, school),
            n.ReadAt is not null,
            n.UpdatedAt))];
    }

    public async Task<int> Handle(MarkMyNoticesReadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid userId, IReadOnlyList<ChildRow> children) = await this.MyChildrenAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<GuardianNotice> unread = children.Count == 0
            ? []
            : await this.notices.ListUnreadUpToAsync(userId, [.. children.Select(c => c.Id)], request.UpTo!.Value.ToUniversalTime(), cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        foreach (GuardianNotice notice in unread)
        {
            notice.MarkRead(now);
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation("Bildirimler okundu: kişi {UserId}, {Count} bildirim", userId, unread.Count);
        return unread.Count;
    }

    public async Task<int> Handle(CountMyUnreadNoticesQuery request, CancellationToken cancellationToken)
    {
        if (this.session.TenantId is null || this.session.UserId is not { } userId)
        {
            return 0;
        }

        IReadOnlyList<RoleAssignment> roles = await this.roleAssignments.ListInCurrentTenantAsync(userId, cancellationToken).ConfigureAwait(false);
        if (!roles.Any(r => r.Role == Role.Guardian))
        {
            return 0;
        }

        (_, IReadOnlyList<ChildRow> children) = await this.MyChildrenAsync(cancellationToken).ConfigureAwait(false);
        return children.Count == 0 ? 0 : await this.notices.CountUnreadAsync(userId, [.. children.Select(c => c.Id)], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Velinin bağlı ve ayrılmamış çocukları ("Çocuklarım" sorgusu: veli değilse 403).</summary>
    private async Task<(Guid UserId, IReadOnlyList<ChildRow> Children)> MyChildrenAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ChildRow> children = await this.sender.Send(new ListMyChildrenQuery(), cancellationToken).ConfigureAwait(false);
        return (this.session.UserId!.Value, children);
    }

    private async Task<Templates> TemplatesAsync() => new(
        await this.localization.CommunicationTextAsync(CommunicationMessages.AbsentText).ConfigureAwait(false),
        await this.localization.CommunicationTextAsync(CommunicationMessages.CorrectedText).ConfigureAwait(false),
        await this.localization.CommunicationTextAsync(CommunicationMessages.AbsentSubject).ConfigureAwait(false),
        await this.localization.CommunicationTextAsync(CommunicationMessages.CorrectedSubject).ConfigureAwait(false),
        await this.localization.CommunicationTextAsync(CommunicationMessages.EmailFooter).ConfigureAwait(false));

    /// <summary>Bildirim ve e-posta şablonları (YAML).</summary>
    private sealed record Templates(string AbsentText, string CorrectedText, string AbsentSubject, string CorrectedSubject, string Footer);
}
