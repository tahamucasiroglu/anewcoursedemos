using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>AI taslağı deposu (spec 0012): kurum filtreli; taslak izlenerek okunur (onay ya da silme için).</summary>
public sealed class AiDraftRepository : IAiDraftRepository
{
    private readonly OkulDbContext context;

    public AiDraftRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<AiDraft?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.AiDrafts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void Add(AiDraft draft) => this.context.AiDrafts.Add(draft);

    public void Remove(AiDraft draft) => this.context.AiDrafts.Remove(draft);
}
