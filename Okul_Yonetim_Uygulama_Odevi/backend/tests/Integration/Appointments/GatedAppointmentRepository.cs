using OkulYonetim.Application.Features.Appointments;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Tests.Integration.Administration;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Gerçek randevu deposunun sarmalayıcısı (spec 0016 AC-6): talebin öğretmen sırası içindeki taze okumaları kapıdan geçer — "dilimde açık
/// randevu var mı" <c>slot</c> kapısından, "çocuk + öğretmen için açık randevu var mı" <c>open</c> kapısından. Sıra varsa ikinci istek kapıya
/// giremez (<see cref="RaceGate.SecondEnteredWhileFirstWaited"/> = false); sıra kaldırılırsa girer. <c>hideActiveOnSlot</c> açıkken
/// dilim denetimi her zaman "yok" der — ikinci savunmayı (kısmi tekil dizin) tek başına sınamak için. Diğer işlemler aynen aktarılır.
/// </summary>
public sealed class GatedAppointmentRepository : IAppointmentRepository
{
    private readonly IAppointmentRepository inner;
    private readonly RaceGate slot;
    private readonly RaceGate open;
    private readonly bool hideActiveOnSlot;

    public GatedAppointmentRepository(IAppointmentRepository inner, RaceGate slot, RaceGate open, bool hideActiveOnSlot = false)
    {
        this.inner = inner;
        this.slot = slot;
        this.open = open;
        this.hideActiveOnSlot = hideActiveOnSlot;
    }

    public async Task<bool> HasActiveOnSlotAsync(Guid slotId, CancellationToken cancellationToken)
    {
        await this.slot.PassAsync().ConfigureAwait(false);
        bool active = await this.inner.HasActiveOnSlotAsync(slotId, cancellationToken).ConfigureAwait(false);
        return active && !this.hideActiveOnSlot;
    }

    public async Task<bool> HasOpenAsync(Guid studentId, Guid teacherUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await this.open.PassAsync().ConfigureAwait(false);
        return await this.inner.HasOpenAsync(studentId, teacherUserId, now, cancellationToken).ConfigureAwait(false);
    }

    public Task<Appointment?> FindAsync(Guid id, bool track, CancellationToken cancellationToken) => this.inner.FindAsync(id, track, cancellationToken);

    public Task<IReadOnlyList<AppointmentListing>> ListActiveForTeacherAsync(Guid teacherUserId, DateTimeOffset from, CancellationToken cancellationToken) =>
        this.inner.ListActiveForTeacherAsync(teacherUserId, from, cancellationToken);

    public Task<IReadOnlyList<AppointmentListing>> ListForGuardianAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, DateTimeOffset after, CancellationToken cancellationToken) =>
        this.inner.ListForGuardianAsync(guardianUserId, studentIds, after, cancellationToken);

    public Task<int> CountPendingAsync(Guid teacherUserId, DateTimeOffset now, CancellationToken cancellationToken) => this.inner.CountPendingAsync(teacherUserId, now, cancellationToken);

    public Task<string> PersonNameAsync(Guid userId, CancellationToken cancellationToken) => this.inner.PersonNameAsync(userId, cancellationToken);

    public void Add(Appointment appointment) => this.inner.Add(appointment);
}
