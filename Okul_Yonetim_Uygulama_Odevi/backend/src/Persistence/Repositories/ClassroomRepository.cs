using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Sınıf deposu: her okuma kurum + şube filtresinden geçer (sınıf, şubesinin görünürlüğünü miras alır).</summary>
public sealed class ClassroomRepository : IClassroomRepository
{
    private readonly OkulDbContext context;

    public ClassroomRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<Classroom>> ListByTermAsync(Guid termId, CancellationToken cancellationToken) =>
        await this.context.Classrooms.Where(c => c.TermId == termId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<Classroom?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.Classrooms.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Classroom>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await this.context.Classrooms.AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<bool> ActiveNameExistsAsync(Guid termId, Guid branchId, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        List<string> names = await this.context.Classrooms
            .Where(c => c.TermId == termId && c.BranchId == branchId && c.IsActive && c.Id != exceptId)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return names.Contains(name.Trim(), TurkishText.IgnoreCase);
    }

    public void Add(Classroom classroom) => this.context.Classrooms.Add(classroom);
}
