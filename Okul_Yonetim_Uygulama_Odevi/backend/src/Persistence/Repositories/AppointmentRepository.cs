using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Appointments;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Randevu deposu (spec 0016): kurum filtreli; liste satırları ad projeksiyonuyla (öğretmen/veli adı kişiden, çocuk adı öğrenci
/// filtresinden — görünmeyen öğrencide boş; 0011 konuşma deposu kalıbı) ve izlenmeden okunur (T7). Açık randevu = onay bekleyen ya da
/// onaylı ve başlamamış.
/// </summary>
public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly OkulDbContext context;

    public AppointmentRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<Appointment?> FindAsync(Guid id, bool track, CancellationToken cancellationToken) =>
        (track ? this.context.Appointments : this.context.Appointments.AsNoTracking())
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> HasActiveOnSlotAsync(Guid slotId, CancellationToken cancellationToken) =>
        this.context.Appointments.AnyAsync(
            a => a.SlotId == slotId && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved),
            cancellationToken);

    public Task<bool> HasOpenAsync(Guid studentId, Guid teacherUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        DateTimeOffset utc = now.ToUniversalTime();
        return this.context.Appointments.AnyAsync(
            a => a.StudentId == studentId && a.TeacherUserId == teacherUserId && a.StartsAt > utc
                && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved),
            cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentListing>> ListActiveForTeacherAsync(Guid teacherUserId, DateTimeOffset from, CancellationToken cancellationToken)
    {
        DateTimeOffset utc = from.ToUniversalTime();
        return await this.Project(this.context.Appointments.AsNoTracking()
                .Where(a => a.TeacherUserId == teacherUserId && a.StartsAt >= utc
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AppointmentListing>> ListForGuardianAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, DateTimeOffset after, CancellationToken cancellationToken)
    {
        DateTimeOffset utc = after.ToUniversalTime();
        List<Guid> ids = [.. studentIds];
        return await this.Project(this.context.Appointments.AsNoTracking()
                .Where(a => (a.GuardianUserId == guardianUserId || ids.Contains(a.StudentId)) && a.StartsAt > utc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> CountPendingAsync(Guid teacherUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        DateTimeOffset utc = now.ToUniversalTime();
        return this.context.Appointments.CountAsync(
            a => a.TeacherUserId == teacherUserId && a.Status == AppointmentStatus.Pending && a.StartsAt > utc,
            cancellationToken);
    }

    public async Task<string> PersonNameAsync(Guid userId, CancellationToken cancellationToken) =>
        await this.context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
            ?? string.Empty;

    public void Add(Appointment appointment) => this.context.Appointments.Add(appointment);

    private IQueryable<AppointmentListing> Project(IQueryable<Appointment> query) =>
        query.Select(a => new AppointmentListing(
            a.Id,
            a.SlotId,
            a.TeacherUserId,
            this.context.Users.Where(u => u.Id == a.TeacherUserId).Select(u => u.FullName).FirstOrDefault() ?? string.Empty,
            a.GuardianUserId,
            this.context.Users.Where(u => u.Id == a.GuardianUserId).Select(u => u.FullName).FirstOrDefault() ?? string.Empty,
            a.StudentId,
            this.context.Students.Where(s => s.Id == a.StudentId).Select(s => s.FullName).FirstOrDefault() ?? string.Empty,
            a.StartsAt,
            a.EndsAt,
            a.Topic,
            a.Status,
            a.Reason));
}
