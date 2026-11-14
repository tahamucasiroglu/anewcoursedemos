using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Repositories.Concrete;

/// <summary>
/// <see cref="IProviderApiKeyRepository"/> EF uygulaması.
/// </summary>
public sealed class ProviderApiKeyRepository : Repository<ProviderApiKey>, IProviderApiKeyRepository
{
    /// <summary>Kurucu.</summary>
    /// <param name="context">DbContext.</param>
    public ProviderApiKeyRepository(ProvidersDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<ProviderApiKey?> FindAsync(Guid userId, string platform, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(k => k.UserId == userId && k.Platform == platform, ct);
}
