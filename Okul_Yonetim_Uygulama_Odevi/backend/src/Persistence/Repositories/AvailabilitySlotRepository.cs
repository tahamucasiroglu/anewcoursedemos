using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Görüşme dilimleri deposu (spec 0016): kurum filtreli, kaldırılmış dilimler filtrede elenir; listeler izlenmez (T7). "Boş" dilim:
/// onay bekleyen ya da onaylı randevusu olmayan.
/// </summary>
public sealed class AvailabilitySlotRepository : IAvailabilitySlotRepository
{
    private readonly OkulDbContext context;

    public AvailabilitySlotRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<AvailabilitySlot?> FindAsync(Guid id, bool track, CancellationToken cancellationToken) =>
        (track ? this.context.AvailabilitySlots : this.context.AvailabilitySlots.AsNoTracking())
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AvailabilitySlot>> ListForTeacherAsync(Guid teacherUserId, DateTimeOffset from, bool inclusive, CancellationToken cancellationToken)
    {
        DateTimeOffset utc = from.ToUniversalTime();
        IQueryable<AvailabilitySlot> query = this.context.AvailabilitySlots.AsNoTracking().Where(s => s.TeacherUserId == teacherUserId);
        query = inclusive ? query.Where(s => s.StartsAt >= utc) : query.Where(s => s.StartsAt > utc);
        return await query.OrderBy(s => s.StartsAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AvailabilitySlot>> ListFreeForTeacherAsync(Guid teacherUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        DateTimeOffset utc = now.ToUniversalTime();
        return await this.context.AvailabilitySlots.AsNoTracking()
            .Where(s => s.TeacherUserId == teacherUserId && s.StartsAt > utc
                && !this.context.Appointments.Any(a => a.SlotId == s.Id && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved)))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(AvailabilitySlot slot) => this.context.AvailabilitySlots.Add(slot);
}
