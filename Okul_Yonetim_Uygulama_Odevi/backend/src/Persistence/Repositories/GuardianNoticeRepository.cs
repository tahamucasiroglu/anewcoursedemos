using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Veli bildirimi deposu (spec 0010): kurum filtreli; velinin listesi yalnızca kendi kişi kimliği ve çağıranın verdiği
/// (bağlı olduğu) öğrencilerle süzülür. Liste izlemesiz (T7).
/// </summary>
public sealed class GuardianNoticeRepository : IGuardianNoticeRepository
{
    private readonly OkulDbContext context;

    public GuardianNoticeRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<GuardianNotice>> ListForStudentsOnDayAsync(IReadOnlyCollection<Guid> studentIds, DateOnly day, CancellationToken cancellationToken) =>
        await this.context.GuardianNotices.Where(n => n.Day == day && studentIds.Contains(n.StudentId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<GuardianNotice>> ListForGuardianAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, int max, CancellationToken cancellationToken) =>
        await this.context.GuardianNotices.AsNoTracking()
            .Where(n => n.GuardianUserId == guardianUserId && studentIds.Contains(n.StudentId))
            .OrderByDescending(n => n.UpdatedAt).ThenByDescending(n => n.Id)
            .Take(max)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> CountUnreadAsync(Guid guardianUserId, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken) =>
        this.context.GuardianNotices.CountAsync(n => n.GuardianUserId == guardianUserId && n.ReadAt == null && studentIds.Contains(n.StudentId), cancellationToken);

    public async Task<IReadOnlyList<GuardianNotice>> ListUnreadAsync(Guid guardianUserId, CancellationToken cancellationToken) =>
        await this.context.GuardianNotices.Where(n => n.GuardianUserId == guardianUserId && n.ReadAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(GuardianNotice notice) => this.context.GuardianNotices.Add(notice);
}
