using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Communication;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetSchoolDay;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListClassroomRoster;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Attendance;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Günlük yoklama (spec 0010): "Sınıflarım", bugünkü liste ve kaydetme. Yetki her istekte <see cref="AttendanceGuard"/> ile.
/// "Bugün" sunucunun Türkiye günüdür (<see cref="SchoolCalendar"/>); istemcinin günü yalnızca doğrulanır — geçmiş gün
/// kilitli, başka gün reddedilir; dönem dışı ve takvimde tatil olan gün "okul günü değil".
/// <para>
/// Kaydetme sınıf + gün başına sıradadır (<c>attendance:&lt;sınıf&gt;:&lt;gün&gt;</c>): eşzamanlı iki kayıttan ikincisi
/// sırayı bekler, ilkinin yazdığını görür ve "değişen öğrenci yok" bulur — tek yoklama, tek bildirim (AC-11). Liste
/// sınıfın o anki kayıtlı öğrencileridir; listede olmayan öğrenci için gelen durum yok sayılır, gelmeyen öğrenci "Var"
/// sayılır (plan Risks 7). Ekranın sürümü güncel değilse ve kayıt bir durumu değiştirecekse 409 (0010 review F-1). Yoklama ve veli bildirimleri (Communication komutu) aynı işlemde yazılır; e-postalar ancak
/// commit'ten sonra kuyruğa girer. Her kayıt/düzeltme audit'li (sınıf, gün, değişen öğrenci sayısı); loglarda yalnızca
/// kimlik ve sayı.
/// </para>
/// </summary>
public sealed class AttendanceHandlers :
    IRequestHandler<ListMyClassroomsQuery, IReadOnlyList<MyClassroomRow>>,
    IRequestHandler<GetTodayAttendanceQuery, AttendanceSheetView>,
    IRequestHandler<SaveTodayAttendanceCommand, AttendanceSaveResult>
{
    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    private readonly AttendanceGuard guard;
    private readonly IClassroomTeacherRepository teachers;
    private readonly IAttendanceRepository attendance;
    private readonly SchoolCalendar calendar;
    private readonly ISender sender;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly IEmailOutbox emailOutbox;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<AttendanceHandlers> logger;

    public AttendanceHandlers(
        AttendanceGuard guard,
        IClassroomTeacherRepository teachers,
        IAttendanceRepository attendance,
        SchoolCalendar calendar,
        ISender sender,
        AdminAudit audit,
        IUnitOfWork unitOfWork,
        IEmailOutbox emailOutbox,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<AttendanceHandlers> logger)
    {
        this.guard = guard;
        this.teachers = teachers;
        this.attendance = attendance;
        this.calendar = calendar;
        this.sender = sender;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.emailOutbox = emailOutbox;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<MyClassroomRow>> Handle(ListMyClassroomsQuery request, CancellationToken cancellationToken)
    {
        (_, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        DateOnly today = this.calendar.Today();
        List<TeacherClassroomRow> mine = [.. (await this.teachers.ListForTeacherAsync(userId, cancellationToken).ConfigureAwait(false))
            .Where(c => c.IsActive && branchIds.Contains(c.BranchId) && c.TermEndDate >= today)];
        if (mine.Count == 0)
        {
            return [];
        }

        HashSet<Guid> taken = [.. await this.attendance.ListTakenClassroomIdsAsync([.. mine.Select(c => c.ClassroomId)], today, cancellationToken).ConfigureAwait(false)];
        return [.. mine
            .OrderBy(c => c.BranchName, TurkishOrder).ThenBy(c => c.Name, TurkishOrder).ThenBy(c => c.ClassroomId)
            .Select(c => new MyClassroomRow(c.ClassroomId, c.Name, c.BranchName, taken.Contains(c.ClassroomId)))];
    }

    public async Task<AttendanceSheetView> Handle(GetTodayAttendanceQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (_, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        ClassroomSummary classroom = await this.guard.RequireClassroomAsync(userId, branchIds, request.ClassroomId, cancellationToken).ConfigureAwait(false);
        SchoolDay day = await this.sender.Send(new GetSchoolDayQuery(classroom.TermId), cancellationToken).ConfigureAwait(false);
        if (!day.IsSchoolDay)
        {
            return new AttendanceSheetView(classroom.Id, classroom.Name, classroom.BranchName, day.Today, false, false, null, [], 0);
        }

        IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(classroom.Id), cancellationToken).ConfigureAwait(false);
        AttendanceSheet? sheet = await this.attendance.FindSheetAsync(classroom.Id, day.Today, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, AttendanceStatus> statuses = sheet is null
            ? []
            : (await this.attendance.ListEntriesAsync(sheet.Id, cancellationToken).ConfigureAwait(false)).ToDictionary(e => e.StudentId, e => e.Status);
        return new AttendanceSheetView(
            classroom.Id,
            classroom.Name,
            classroom.BranchName,
            day.Today,
            true,
            sheet is not null,
            sheet is null ? null : this.Clock(sheet.UpdatedAt),
            [.. roster.Select(s => new AttendanceStudentRow(s.Id, s.FullName, statuses.GetValueOrDefault(s.Id, AttendanceStatus.Present).ToString()))],
            sheet?.Revision ?? 0);
    }

    public async Task<AttendanceSaveResult> Handle(SaveTodayAttendanceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        ClassroomSummary classroom = await this.guard.RequireClassroomAsync(userId, branchIds, request.ClassroomId, cancellationToken).ConfigureAwait(false);
        SchoolDay day = await this.sender.Send(new GetSchoolDayQuery(classroom.TermId), cancellationToken).ConfigureAwait(false);
        if (request.Day!.Value != day.Today)
        {
            throw new BusinessException(await this.localization.AttendanceTextAsync(
                request.Day.Value < day.Today ? AttendanceMessages.PastDayLocked : AttendanceMessages.OnlyToday).ConfigureAwait(false));
        }

        if (!day.IsSchoolDay)
        {
            throw new BusinessException(await this.localization.AttendanceTextAsync(AttendanceMessages.NotSchoolDay).ConfigureAwait(false));
        }

        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync(
                "attendance:" + classroom.Id.ToString("N") + ":" + day.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.AttendanceTextAsync(AttendanceMessages.TooManyRequests).ConfigureAwait(false));
        }

        await using (scope.ConfigureAwait(false))
        {
            Dictionary<Guid, AttendanceStatus> requested = request.Entries!.ToDictionary(
                e => e.StudentId,
                e => SaveTodayAttendanceCommandValidator.TryParseStatus(e.Status, out AttendanceStatus s) ? s : AttendanceStatus.Present);
            IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(classroom.Id), cancellationToken).ConfigureAwait(false);
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            AttendanceSheet? sheet = await this.attendance.FindSheetAsync(classroom.Id, day.Today, cancellationToken).ConfigureAwait(false);
            bool isNew = sheet is null;
            Dictionary<Guid, AttendanceEntry> existing = sheet is null
                ? []
                : (await this.attendance.ListEntriesAsync(sheet.Id, cancellationToken).ConfigureAwait(false)).ToDictionary(e => e.StudentId);

            // İyimser eşzamanlılık (0010 review F-1): ekran açıldıktan sonra başkası kaydettiyse ve bu kayıt bir öğrencinin
            // durumunu değiştirecekse eski ekran üzerine yazamaz — hiçbir şey yazılmaz, istemci listeyi yeniler. Aynı durumları
            // gönderen eski ekran (iki öğretmen aynı yoklamayı aldı) bir şeyi geri almadığı için kabul edilir (AC-11).
            int current = sheet?.Revision ?? 0;
            if (request.Version!.Value != current
                && roster.Any(s => !existing.TryGetValue(s.Id, out AttendanceEntry? e) || e.Status != requested.GetValueOrDefault(s.Id, AttendanceStatus.Present)))
            {
                this.logger.LogInformation(
                    "Yoklama eski ekrandan kaydedilmek istendi, reddedildi: sınıf {ClassroomId}, gün {Day}, ekran sürümü {Version}, güncel sürüm {Current}",
                    classroom.Id,
                    day.Today,
                    request.Version.Value,
                    current);
                throw new ConflictException(await this.localization.AttendanceTextAsync(AttendanceMessages.StaleSheet).ConfigureAwait(false));
            }

            if (sheet is null)
            {
                sheet = AttendanceSheet.Start(tenantId, classroom.BranchId, classroom.Id, day.Today, userId, now);
                this.attendance.AddSheet(sheet);
            }
            else
            {
                sheet.Touch(userId, now);
            }

            List<AttendanceChange> changes = [];
            List<Guid> absent = [];
            int changed = 0;
            int present = 0;
            int late = 0;
            foreach (RosterStudent student in roster)
            {
                AttendanceStatus status = requested.GetValueOrDefault(student.Id, AttendanceStatus.Present);
                AttendanceStatus? previous = existing.TryGetValue(student.Id, out AttendanceEntry? entry) ? entry.Status : null;
                if (entry is null)
                {
                    this.attendance.AddEntry(AttendanceEntry.Create(sheet, student.Id, status, now));
                }
                else if (entry.Status != status)
                {
                    entry.Change(status);
                }

                if (previous != status)
                {
                    changed++;
                }

                if ((previous == AttendanceStatus.Absent) != (status == AttendanceStatus.Absent))
                {
                    changes.Add(new AttendanceChange(student.Id, student.FullName, status == AttendanceStatus.Absent));
                }

                switch (status)
                {
                    case AttendanceStatus.Absent:
                        absent.Add(student.Id);
                        break;
                    case AttendanceStatus.Late:
                        late++;
                        break;
                    default:
                        present++;
                        break;
                }
            }

            AttendanceNoticeResult notices = await this.sender.Send(new NotifyAttendanceChangesCommand(day.Today, changes, absent), cancellationToken).ConfigureAwait(false);
            this.audit.Record(
                isNew ? AuditAction.AttendanceRecorded : AuditAction.AttendanceCorrected,
                tenantId,
                userId,
                nameof(AttendanceSheet),
                sheet.Id,
                string.Create(CultureInfo.InvariantCulture, $"classroom={classroom.Id:D};day={day.Today:yyyy-MM-dd};changed={changed}"));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);

            // Kayıt kalıcı olduktan sonra kuyruğa (spec 0006 kalıbı): geri alınan yoklama e-posta göndermez.
            foreach (EmailMessage email in notices.Emails)
            {
                this.emailOutbox.Enqueue(email);
            }

            this.logger.LogInformation(
                "Yoklama kaydedildi: sınıf {ClassroomId}, yoklama {SheetId}, gün {Day}, değişen {Changed}, yok {Absent}, bildirim giden veli {Notified}",
                classroom.Id,
                sheet.Id,
                day.Today,
                changed,
                absent.Count,
                notices.NotifiedGuardians);
            return new AttendanceSaveResult(present, absent.Count, late, notices.NotifiedGuardians, notices.StudentsWithoutGuardian, this.Clock(now), sheet.Revision);
        }
    }

    private string Clock(DateTimeOffset instant) =>
        this.calendar.TurkeyTime(instant).ToString("HH:mm", CultureInfo.InvariantCulture);
}
