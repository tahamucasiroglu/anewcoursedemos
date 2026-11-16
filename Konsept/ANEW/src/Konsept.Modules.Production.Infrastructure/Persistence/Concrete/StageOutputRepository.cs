using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>Aşama çıktıları deposu (spec 0017): konsept başına tek satır; yalnızca sahibinin satırı okunur/yazılır (BR-1).</summary>
public sealed class StageOutputRepository : EfRepositoryBase<ProductionDbContext>, IStageOutputRepository
{
    /// <summary>Initializes a new instance of the <see cref="StageOutputRepository"/> class.</summary>
    /// <param name="context">Bağlam.</param>
    public StageOutputRepository(ProductionDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<StageOutputs?> GetAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        Context.StageOutputs.AsNoTracking().FirstOrDefaultAsync(o => o.ConceptId == conceptId && o.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public async Task UpsertAsync(StageOutputs outputs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        var existing = await Context.StageOutputs.AsNoTracking()
            .AnyAsync(o => o.ConceptId == outputs.ConceptId, cancellationToken).ConfigureAwait(false);
        if (existing)
        {
            if (!await Context.StageOutputs.AnyAsync(o => o.ConceptId == outputs.ConceptId && o.UserId == outputs.UserId, cancellationToken).ConfigureAwait(false))
            {
                return; // Başkasının satırı değişmez.
            }

            Context.StageOutputs.Update(outputs);
        }
        else
        {
            Context.StageOutputs.Add(outputs);
        }

        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Context.Entry(outputs).State = EntityState.Detached;
    }
}
