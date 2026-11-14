using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Repositories.Concrete;

/// <summary>
/// <see cref="IBudgetSettingRepository"/> EF uygulaması.
/// </summary>
public sealed class BudgetSettingRepository : Repository<BudgetSetting>, IBudgetSettingRepository
{
    /// <summary>Kurucu.</summary>
    /// <param name="context">DbContext.</param>
    public BudgetSettingRepository(ProvidersDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<BudgetSetting?> FindByUserAsync(Guid userId, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(b => b.UserId == userId, ct);
}
