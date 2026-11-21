using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Appointments;

namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>
/// Müsaitlik ve "Takvimim" (spec 0016 gereksinim 1–2). Yetki <see cref="AppointmentsGuard"/>'da: Öğretmen rolü yoksa 403; başka
/// öğretmenin ya da okulun dilimi 404. Açma ve kaldırma öğretmen sırasında (<see cref="AppointmentLock"/>): çakışma ve "talebi var mı"
/// kararları sıra içinde taze okumayla verilir. Dilim hesabı ve Türkiye saati çevirisi <see cref="AvailabilityPlanner"/>'da. Açma ve
/// kaldırma audit'e değil Information loguna yazılır (yalnızca kimlikler ve sayı — gereksinim 10).
/// </summary>
public sealed class AvailabilityHandlers :
    IRequestHandler<OpenAvailabilityCommand, AvailabilityOpened>,
    IRequestHandler<RemoveSlotCommand, Guid>,
    IRequestHandler<GetMyScheduleQuery, TeacherSchedule>,
    IRequestHandler<CountMyPendingAppointmentsQuery, int>
{
    private readonly AppointmentsGuard guard;
    private readonly IAvailabilitySlotRepository slots;
    private readonly IAppointmentRepository appointments;
    private readonly SchoolCalendar calendar;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentSession session;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<AvailabilityHandlers> logger;

    public AvailabilityHandlers(
        AppointmentsGuard guard,
        IAvailabilitySlotRepository slots,
        IAppointmentRepository appointments,
        SchoolCalendar calendar,
        IUnitOfWork unitOfWork,
        ICurrentSession session,
        TimeProvider timeProvider,
        ILogger<AvailabilityHandlers> logger)
    {
        this.guard = guard;
        this.slots = slots;
        this.appointments = appointments;
        this.calendar = calendar;
        this.unitOfWork = unitOfWork;
        this.session = session;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<AvailabilityOpened> Handle(OpenAvailabilityCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AppointmentsCaller caller = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        _ = AppointmentTimeText.TryParse(request.Start, out TimeOnly start);
        _ = AppointmentTimeText.TryParse(request.End, out TimeOnly end);
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        IReadOnlyList<SlotWindow> windows = AvailabilityPlanner.Plan(request.Day!.Value, start, end, request.DurationMinutes!.Value, this.calendar.Today(), this.calendar.Zone, now);
        if (windows.Count == 0)
        {
            throw await this.guard.BusinessAsync(AppointmentsMessages.NoSlotsLeft).ConfigureAwait(false);
        }

        IExclusiveScope scope = await AppointmentLock.EnterAsync(this.unitOfWork, this.guard, caller.UserId, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            IReadOnlyList<AvailabilitySlot> existing = await this.slots.ListForTeacherAsync(caller.UserId, now, false, cancellationToken).ConfigureAwait(false);
            if (windows.Any(w => existing.Any(s => w.Overlaps(s.StartsAt, s.EndsAt))))
            {
                throw await this.guard.BusinessAsync(AppointmentsMessages.Overlap).ConfigureAwait(false);
            }

            foreach (SlotWindow window in windows)
            {
                this.slots.Add(AvailabilitySlot.Open(caller.TenantId, caller.UserId, window, now));
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        this.logger.LogInformation(
            "Müsaitlik açıldı: okul {TenantId}, öğretmen {TeacherId}, dilim {SlotCount}",
            caller.TenantId,
            caller.UserId,
            windows.Count);
        return new AvailabilityOpened(windows.Count);
    }

    public async Task<Guid> Handle(RemoveSlotCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AppointmentsCaller caller = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        AvailabilitySlot? seen = await this.slots.FindAsync(request.SlotId, false, cancellationToken).ConfigureAwait(false);
        if (seen is null || seen.TeacherUserId != caller.UserId)
        {
            throw await this.guard.NotFoundAsync(AppointmentsMessages.SlotNotFound).ConfigureAwait(false);
        }

        IExclusiveScope scope = await AppointmentLock.EnterAsync(this.unitOfWork, this.guard, caller.UserId, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            AvailabilitySlot slot = await this.slots.FindAsync(request.SlotId, true, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(AppointmentsMessages.SlotNotFound).ConfigureAwait(false);
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            if (await this.appointments.HasActiveOnSlotAsync(slot.Id, cancellationToken).ConfigureAwait(false))
            {
                throw await this.guard.BusinessAsync(AppointmentsMessages.SlotHasRequest).ConfigureAwait(false);
            }

            if (slot.HasStarted(now))
            {
                throw await this.guard.BusinessAsync(AppointmentsMessages.SlotStartedRemove).ConfigureAwait(false);
            }

            slot.Remove(now);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        this.logger.LogInformation(
            "Görüşme saati kaldırıldı: okul {TenantId}, öğretmen {TeacherId}, dilim {SlotId}",
            caller.TenantId,
            caller.UserId,
            request.SlotId);
        return request.SlotId;
    }

    public async Task<TeacherSchedule> Handle(GetMyScheduleQuery request, CancellationToken cancellationToken)
    {
        AppointmentsCaller caller = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        DateTimeOffset dayStart = AvailabilityPlanner.ToInstant(this.calendar.Today(), TimeOnly.MinValue, this.calendar.Zone);
        IReadOnlyList<AvailabilitySlot> mine = await this.slots.ListForTeacherAsync(caller.UserId, dayStart, true, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, AppointmentListing> active = (await this.appointments.ListActiveForTeacherAsync(caller.UserId, dayStart, cancellationToken).ConfigureAwait(false))
            .GroupBy(a => a.SlotId)
            .ToDictionary(g => g.Key, g => g.First());
        List<ScheduleItem> items = [.. mine.Select(s => Item(s, active.GetValueOrDefault(s.Id), now))];
        List<ScheduleItem> pending = [.. items.Where(i => i.State == nameof(AppointmentState.Pending))];
        return new TeacherSchedule(pending.Count, pending, items);
    }

    public async Task<int> Handle(CountMyPendingAppointmentsQuery request, CancellationToken cancellationToken)
    {
        if (this.session.TenantId is null || this.session.UserId is null)
        {
            return 0;
        }

        AppointmentsCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        return caller.IsTeacher
            ? await this.appointments.CountPendingAsync(caller.UserId, this.timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false)
            : 0;
    }

    private static ScheduleItem Item(AvailabilitySlot slot, AppointmentListing? appointment, DateTimeOffset now)
    {
        string state = appointment is null
            ? "Free"
            : appointment.Status == AppointmentStatus.Pending && appointment.StartsAt <= now
                ? nameof(AppointmentState.Expired)
                : appointment.Status.ToString();
        return new ScheduleItem(
            slot.Id,
            slot.StartsAt,
            slot.EndsAt,
            state,
            slot.HasStarted(now),
            appointment?.Id,
            appointment?.GuardianName,
            appointment?.StudentName,
            appointment?.Topic);
    }
}
