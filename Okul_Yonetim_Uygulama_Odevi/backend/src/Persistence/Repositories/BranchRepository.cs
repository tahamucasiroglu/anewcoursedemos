using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Şube deposu: her okuma kurum + şube filtresinden geçer (filtre dışı okuma yok).</summary>
public sealed class BranchRepository : IBranchRepository
{
    private readonly OkulDbContext context;

    public BranchRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<Branch>> ListVisibleAsync(CancellationToken cancellationToken) =>
        await this.context.Branches.ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<Branch?> FindVisibleAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public void Add(Branch branch) => this.context.Branches.Add(branch);
}
