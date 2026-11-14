using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Repositories.Concrete;

/// <summary>
/// <see cref="IModelPriceRepository"/> EF uygulaması.
/// </summary>
public sealed class ModelPriceRepository : Repository<ModelPrice>, IModelPriceRepository
{
    /// <summary>Kurucu.</summary>
    /// <param name="context">DbContext.</param>
    public ModelPriceRepository(ProvidersDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<ModelPrice?> FindEffectiveAsync(Guid userId, string modelKey, DateTimeOffset atUtc, CancellationToken ct)
    {
        var at = atUtc.ToUniversalTime();
        return Set.AsNoTracking()
            .Where(p => p.UserId == userId && p.ModelKey == modelKey && p.EffectiveFromUtc <= at)
            .OrderByDescending(p => p.EffectiveFromUtc)
            .FirstOrDefaultAsync(ct);
    }
}
