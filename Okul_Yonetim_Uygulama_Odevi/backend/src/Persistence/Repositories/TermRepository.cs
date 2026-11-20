using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Dönem deposu: her okuma kurum filtresinden geçer (filtre dışı okuma yok).</summary>
public sealed class TermRepository : ITermRepository
{
    private readonly OkulDbContext context;

    public TermRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<Term>> ListAsync(CancellationToken cancellationToken) =>
        await this.context.Terms.ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<Term?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.Terms.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        List<string> names = await this.context.Terms.Where(t => t.Id != exceptId).Select(t => t.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return names.Contains(name.Trim(), TurkishText.IgnoreCase);
    }

    public void Add(Term term) => this.context.Terms.Add(term);
}
