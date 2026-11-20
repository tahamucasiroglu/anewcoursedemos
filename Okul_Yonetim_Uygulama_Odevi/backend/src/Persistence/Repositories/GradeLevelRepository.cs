using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Sınıf seviyesi deposu: her okuma kurum filtresinden geçer (filtre dışı okuma yok).</summary>
public sealed class GradeLevelRepository : IGradeLevelRepository
{
    private readonly OkulDbContext context;

    public GradeLevelRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<GradeLevel>> ListAsync(CancellationToken cancellationToken) =>
        await this.context.GradeLevels.ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<GradeLevel?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.GradeLevels.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    // Türkçe harf kuralıyla karşılaştırma (şube adıyla aynı gerekçe): okul başına seviye sayısı küçük.
    public async Task<bool> ActiveNameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        List<string> names = await this.context.GradeLevels.Where(g => g.IsActive && g.Id != exceptId).Select(g => g.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return names.Contains(name.Trim(), TurkishText.IgnoreCase);
    }

    public void Add(GradeLevel gradeLevel) => this.context.GradeLevels.Add(gradeLevel);
}
