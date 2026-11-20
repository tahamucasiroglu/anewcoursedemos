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

    // Türkçe harf kuralıyla karşılaştırma (ı/I, i/İ, ö/Ö): veritabanının yerel ayarı Türkçe büyük/küçük harfi bilmeyebilir;
    // okul başına şube sayısı küçük olduğundan adlar okunup uygulamada karşılaştırılır.
    public async Task<bool> ActiveNameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        List<string> names = await this.context.Branches.Where(b => b.IsActive && b.Id != exceptId).Select(b => b.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return names.Contains(name.Trim(), TurkishText.IgnoreCase);
    }

    public Task<int> CountActiveAsync(CancellationToken cancellationToken) =>
        this.context.Branches.CountAsync(b => b.IsActive, cancellationToken);

    public void Add(Branch branch) => this.context.Branches.Add(branch);
}
