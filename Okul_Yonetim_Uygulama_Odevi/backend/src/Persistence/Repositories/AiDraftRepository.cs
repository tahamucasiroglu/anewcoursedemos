using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>AI taslağı deposu (spec 0012): kurum filtreli; taslak izlenerek okunur (onay ya da silme için). Spec 0014: haftalık özet bulma (izlenen) ve sınıfın haftalık durumları (izlemesiz projeksiyon).</summary>
public sealed class AiDraftRepository : IAiDraftRepository
{
    private readonly OkulDbContext context;

    public AiDraftRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<AiDraft?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.AiDrafts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<AiDraft?> FindWeeklySummaryAsync(Guid studentId, DateOnly weekStart, CancellationToken cancellationToken) =>
        this.context.AiDrafts.FirstOrDefaultAsync(d => d.Kind == AiDraftKind.WeeklySummary && d.StudentId == studentId && d.PeriodStart == weekStart, cancellationToken);

    public async Task<IReadOnlyList<(Guid StudentId, AiDraftStatus Status)>> ListWeeklySummaryStatusesAsync(IReadOnlyCollection<Guid> studentIds, DateOnly weekStart, CancellationToken cancellationToken)
    {
        var rows = await this.context.AiDrafts.AsNoTracking()
            .Where(d => d.Kind == AiDraftKind.WeeklySummary && d.PeriodStart == weekStart && studentIds.Contains(d.StudentId))
            .Select(d => new { d.StudentId, d.Status })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => (r.StudentId, r.Status))];
    }

    public void Add(AiDraft draft) => this.context.AiDrafts.Add(draft);

    public void Remove(AiDraft draft) => this.context.AiDrafts.Remove(draft);
}
