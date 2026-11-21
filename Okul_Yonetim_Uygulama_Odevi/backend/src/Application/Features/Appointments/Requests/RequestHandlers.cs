using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Administration.ClassroomTeachers;
using OkulYonetim.Application.Features.Communication;
using OkulYonetim.Application.Features.Communication.Notifications;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>
/// Veli talebi, öğretmen yanıtı, iptal ve "Randevularım" (spec 0016 gereksinim 3–9). Yetki <see cref="AppointmentsGuard"/>'da: rol yoksa
/// 403, ilişki yoksa 404 (çocuğun öteki velisi randevuyu görür ama iptal edemez: 403).
/// <para>
/// Bütün değişiklikler dilimin öğretmeninin sırasında (<see cref="AppointmentLock"/>) taze okumayla karar verir: dilim boş mu, başlamış
/// mı, çocuk + öğretmen için açık randevu var mı, talep hâlâ onay bekliyor mu. Dilimde tek açık randevu kuralının ikinci katmanı kısmi
/// tekil dizindir: ihlal (<see cref="ConcurrencyConflictException"/>) "Bu saat artık dolu." iş hatasına çevrilir. Randevu, veli
/// bildirimleri (<see cref="AppointmentNotifications"/>) ve audit tek işlemde yazılır; bildirimler ancak commit'ten sonra kapıdan çıkar
/// (gönderici hatası işlemi geri almaz). Loglarda ve audit'te yalnızca kimlikler; konu, neden ve adlar yok (gereksinim 9–10).
/// </para>
/// </summary>
public sealed class RequestHandlers :
    IRequestHandler<ListBookableTeachersQuery, IReadOnlyList<BookableTeacher>>,
    IRequestHandler<ListOpenSlotsQuery, IReadOnlyList<OpenSlot>>,
    IRequestHandler<RequestAppointmentCommand, AppointmentChanged>,
    IRequestHandler<RespondAppointmentCommand, AppointmentChanged>,
    IRequestHandler<CancelAppointmentCommand, AppointmentChanged>,
    IRequestHandler<ListMyAppointmentsQuery, MyAppointments>
{
    /// <summary>"Randevularım"da geçmiş randevuların gösterildiği gün sayısı.</summary>
    public const int PastDays = 30;

    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    private readonly AppointmentsGuard guard;
    private readonly IAvailabilitySlotRepository slots;
    private readonly IAppointmentRepository appointments;
    private readonly AppointmentNotifications notifications;
    private readonly ISender sender;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<RequestHandlers> logger;

    public RequestHandlers(
        AppointmentsGuard guard,
        IAvailabilitySlotRepository slots,
        IAppointmentRepository appointments,
        AppointmentNotifications notifications,
        ISender sender,
        AdminAudit audit,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<RequestHandlers> logger)
    {
        this.guard = guard;
        this.slots = slots;
        this.appointments = appointments;
        this.notifications = notifications;
        this.sender = sender;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<BookableTeacher>> Handle(ListBookableTeachersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AppointmentsCaller caller = await this.guard.RequireGuardianAsync(cancellationToken).ConfigureAwait(false);
        await this.guard.RequireChildAsync(caller, request.StudentId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<TeacherOption> teachers = await this.guard.ChildTeachersAsync(request.StudentId, caller.UserId, cancellationToken).ConfigureAwait(false);
        return [.. teachers.OrderBy(t => t.FullName, TurkishOrder).ThenBy(t => t.UserId).Select(t => new BookableTeacher(t.UserId, t.FullName))];
    }

    public async Task<IReadOnlyList<OpenSlot>> Handle(ListOpenSlotsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AppointmentsCaller caller = await this.guard.RequireGuardianAsync(cancellationToken).ConfigureAwait(false);
        await this.guard.RequireRelatedTeacherAsync(caller, request.StudentId, request.TeacherUserId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AvailabilitySlot> free = await this.slots.ListFreeForTeacherAsync(request.TeacherUserId, this.timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        return [.. free.Select(s => new OpenSlot(s.Id, s.StartsAt, s.EndsAt))];
    }

    public async Task<AppointmentChanged> Handle(RequestAppointmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AppointmentsCaller caller = await this.guard.RequireGuardianAsync(cancellationToken).ConfigureAwait(false);
        Guid slotId = request.SlotId!.Value;
        Guid studentId = request.StudentId!.Value;
        AvailabilitySlot seen = await this.slots.FindAsync(slotId, false, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(AppointmentsMessages.SlotNotFound).ConfigureAwait(false);
        await this.guard.RequireRelatedTeacherAsync(caller, studentId, seen.TeacherUserId, cancellationToken).ConfigureAwait(false);

        Appointment appointment;
        IExclusiveScope scope = await AppointmentLock.EnterAsync(this.unitOfWork, this.guard, seen.TeacherUserId, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            AvailabilitySlot slot = await this.slots.FindAsync(slotId, true, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.BusinessAsync(AppointmentsMessages.SlotTaken).ConfigureAwait(false);
            if (slot.HasStarted(now))
            {
                throw await this.guard.BusinessAsync(AppointmentsMessages.SlotStarted).ConfigureAwait(false);
            }

            if (await this.appointments.HasActiveOnSlotAsync(slot.Id, cancellationToken).ConfigureAwait(false))
            {
                throw await this.guard.BusinessAsync(AppointmentsMessages.SlotTaken).ConfigureAwait(false);
            }

            if (await this.appointments.HasOpenAsync(studentId, slot.TeacherUserId, now, cancellationToken).ConfigureAwait(false))
            {
                throw await this.guard.BusinessAsync(AppointmentsMessages.OpenAppointmentExists).ConfigureAwait(false);
            }

            appointment = Appointment.Request(slot, caller.UserId, studentId, request.Topic!, now);
            this.appointments.Add(appointment);
            this.audit.Record(AuditAction.AppointmentRequested, caller.TenantId, caller.UserId, nameof(Appointment), appointment.Id, Detail(appointment));
            try
            {
                await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ConcurrencyConflictException)
            {
                // Kısmi tekil dizin (ikinci savunma): aynı dilime açık bir randevu başka bir istekle yazıldı.
                this.logger.LogWarning("Randevu talebi dilim tekil dizinine takıldı: dilim {SlotId}, veli {UserId}", slot.Id, caller.UserId);
                throw await this.guard.BusinessAsync(AppointmentsMessages.SlotTaken).ConfigureAwait(false);
            }

            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        bool notified = await this.notifications.ToTeacherAsync(appointment, AppointmentsMessages.NewRequestNotice, cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation(
            "Randevu talep edildi: randevu {AppointmentId}, dilim {SlotId}, öğretmen {TeacherId}, veli {GuardianId}, öğrenci {StudentId}, öğretmene bildirim {Notified}",
            appointment.Id,
            appointment.SlotId,
            appointment.TeacherUserId,
            appointment.GuardianUserId,
            appointment.StudentId,
            notified);
        return new AppointmentChanged(appointment.Id, appointment.Status.ToString());
    }

    public async Task<AppointmentChanged> Handle(RespondAppointmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AppointmentsCaller caller = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        Appointment? seen = await this.appointments.FindAsync(request.AppointmentId, false, cancellationToken).ConfigureAwait(false);
        if (seen is null || seen.TeacherUserId != caller.UserId)
        {
            throw await this.guard.NotFoundAsync(AppointmentsMessages.AppointmentNotFound).ConfigureAwait(false);
        }

        bool approve = request.Decision == RespondAppointmentCommandValidator.Approve;
        Appointment appointment;
        SchoolNoticeResult notices;
        IExclusiveScope scope = await AppointmentLock.EnterAsync(this.unitOfWork, this.guard, caller.UserId, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            appointment = await this.appointments.FindAsync(request.AppointmentId, true, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(AppointmentsMessages.AppointmentNotFound).ConfigureAwait(false);
            if (appointment.Status != AppointmentStatus.Pending)
            {
                throw await this.guard.BusinessAsync(AppointmentsMessages.AlreadyResponded).ConfigureAwait(false);
            }

            if (appointment.HasStarted(now))
            {
                throw await this.guard.BusinessAsync(AppointmentsMessages.RequestExpired).ConfigureAwait(false);
            }

            if (approve)
            {
                appointment.Approve(now);
                notices = await this.notifications.ToGuardiansAsync(appointment, AppointmentsMessages.ApprovedNotice, null, appointment.Id, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                appointment.Reject(request.Reason, now);
                notices = await this.notifications.ToGuardiansAsync(
                    appointment,
                    appointment.Reason is null ? AppointmentsMessages.RejectedNotice : AppointmentsMessages.RejectedWithReasonNotice,
                    appointment.Reason,
                    appointment.Id,
                    cancellationToken).ConfigureAwait(false);
            }

            this.audit.Record(
                approve ? AuditAction.AppointmentApproved : AuditAction.AppointmentRejected,
                caller.TenantId,
                caller.UserId,
                nameof(Appointment),
                appointment.Id,
                Detail(appointment));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        // Kayıt kalıcı olduktan sonra Communication'ın bildirim kapısına: veliye giden her şey tek çıkıştan geçer (spec 0011).
        await this.sender.Send(new DeliverNotificationsCommand(notices.Pushes, []), cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation(
            "Randevu talebi yanıtlandı: randevu {AppointmentId}, öğretmen {TeacherId}, durum {Status}, bildirim giden veli {Notified}",
            appointment.Id,
            caller.UserId,
            appointment.Status,
            notices.NotifiedGuardians);
        return new AppointmentChanged(appointment.Id, appointment.Status.ToString());
    }

    public async Task<AppointmentChanged> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AppointmentsCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (!caller.IsTeacher && !caller.IsGuardian)
        {
            throw await this.guard.ForbiddenAsync(AppointmentsMessages.Forbidden).ConfigureAwait(false);
        }

        Appointment seen = await this.appointments.FindAsync(request.AppointmentId, false, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(AppointmentsMessages.AppointmentNotFound).ConfigureAwait(false);
        bool asTeacher = caller.IsTeacher && seen.TeacherUserId == caller.UserId;
        if (!asTeacher && seen.GuardianUserId != caller.UserId)
        {
            bool parentOfChild = (await this.guard.ChildrenAsync(caller, cancellationToken).ConfigureAwait(false)).Any(c => c.Id == seen.StudentId);
            throw parentOfChild
                ? await this.guard.ForbiddenAsync(AppointmentsMessages.RequesterOnly).ConfigureAwait(false)
                : await this.guard.NotFoundAsync(AppointmentsMessages.AppointmentNotFound).ConfigureAwait(false);
        }

        if (!asTeacher && !caller.IsGuardian)
        {
            throw await this.guard.NotFoundAsync(AppointmentsMessages.AppointmentNotFound).ConfigureAwait(false);
        }

        Appointment appointment;
        SchoolNoticeResult notices = new([], 0);
        IExclusiveScope scope = await AppointmentLock.EnterAsync(this.unitOfWork, this.guard, seen.TeacherUserId, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            appointment = await this.appointments.FindAsync(request.AppointmentId, true, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(AppointmentsMessages.AppointmentNotFound).ConfigureAwait(false);
            if (appointment.HasStarted(now))
            {
                throw await this.guard.BusinessAsync(AppointmentsMessages.StartedCannotCancel).ConfigureAwait(false);
            }

            if (asTeacher)
            {
                if (appointment.Status == AppointmentStatus.Pending)
                {
                    throw await this.guard.BusinessAsync(AppointmentsMessages.RejectInsteadOfCancel).ConfigureAwait(false);
                }

                if (!appointment.CanCancelByTeacher(now))
                {
                    throw await this.guard.BusinessAsync(AppointmentsMessages.CannotCancel).ConfigureAwait(false);
                }

                appointment.CancelByTeacher(request.Reason, now);
                AvailabilitySlot? slot = await this.slots.FindAsync(appointment.SlotId, true, cancellationToken).ConfigureAwait(false);
                slot?.Remove(now);
                notices = await this.notifications.ToGuardiansAsync(
                    appointment,
                    appointment.Reason is null ? AppointmentsMessages.TeacherCancelledNotice : AppointmentsMessages.TeacherCancelledWithReasonNotice,
                    appointment.Reason,
                    appointment.SlotId,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (!appointment.CanCancelByGuardian(now))
                {
                    throw await this.guard.BusinessAsync(AppointmentsMessages.CannotCancel).ConfigureAwait(false);
                }

                appointment.CancelByGuardian(now);
            }

            this.audit.Record(AuditAction.AppointmentCancelled, caller.TenantId, caller.UserId, nameof(Appointment), appointment.Id, Detail(appointment));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        bool teacherNotified = false;
        if (asTeacher)
        {
            await this.sender.Send(new DeliverNotificationsCommand(notices.Pushes, []), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            teacherNotified = await this.notifications.ToTeacherAsync(appointment, AppointmentsMessages.GuardianCancelledNotice, cancellationToken).ConfigureAwait(false);
        }

        this.logger.LogInformation(
            "Randevu iptal edildi: randevu {AppointmentId}, iptal eden {UserId}, durum {Status}, bildirim giden veli {Notified}, öğretmene bildirim {TeacherNotified}",
            appointment.Id,
            caller.UserId,
            appointment.Status,
            notices.NotifiedGuardians,
            teacherNotified);
        return new AppointmentChanged(appointment.Id, appointment.Status.ToString());
    }

    public async Task<MyAppointments> Handle(ListMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        AppointmentsCaller caller = await this.guard.RequireGuardianAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<ChildRow> children = await this.guard.ChildrenAsync(caller, cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        IReadOnlyList<AppointmentListing> rows = await this.appointments.ListForGuardianAsync(caller.UserId, [.. children.Select(c => c.Id)], now.AddDays(-PastDays), cancellationToken).ConfigureAwait(false);
        List<MyAppointmentRow> mapped = [.. rows.Select(r => Row(r, caller.UserId, now))];
        return new MyAppointments(
            [.. mapped.Where(r => r.StartsAt > now).OrderBy(r => r.StartsAt).ThenBy(r => r.Id)],
            [.. mapped.Where(r => r.StartsAt <= now).OrderByDescending(r => r.StartsAt).ThenBy(r => r.Id)]);
    }

    private static MyAppointmentRow Row(AppointmentListing r, Guid userId, DateTimeOffset now)
    {
        bool mine = r.GuardianUserId == userId;
        bool started = r.StartsAt <= now;
        AppointmentState state = r.Status == AppointmentStatus.Pending && started ? AppointmentState.Expired : (AppointmentState)(int)r.Status;
        bool open = !started && r.Status is AppointmentStatus.Pending or AppointmentStatus.Approved;
        return new MyAppointmentRow(
            r.Id,
            r.TeacherUserId,
            r.TeacherName,
            r.StudentId,
            r.StudentName,
            r.StartsAt,
            r.EndsAt,
            state.ToString(),
            r.Reason,
            mine,
            r.GuardianName,
            mine && open);
    }

    /// <summary>Audit ayrıntısı: yalnızca kimlikler ve durum (konu, neden ve ad yok — gereksinim 9).</summary>
    private static string Detail(Appointment appointment) =>
        string.Create(CultureInfo.InvariantCulture, $"slot={appointment.SlotId:D};student={appointment.StudentId:D};teacher={appointment.TeacherUserId:D};status={appointment.Status}");
}
