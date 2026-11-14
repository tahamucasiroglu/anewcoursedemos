using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Domain.Entities;

namespace TahaKonsept.Modules.Agents.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IAgentDeviceRepository"/> EF uygulaması.
/// </summary>
public sealed class AgentDeviceRepository : Repository<AgentDevice>, IAgentDeviceRepository
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">DbContext.</param>
    public AgentDeviceRepository(AgentsDbContext context)
        : base(context)
    {
    }

    /// <summary>İptal edilmemiş cihazlar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihazlar.</returns>
    public async Task<IReadOnlyList<AgentDevice>> ListActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Query()
            .Where(d => d.UserId == userId && !d.IsRevoked)
            .OrderByDescending(d => d.PairedAtUtc)
            .ToListAsync(cancellationToken);

    /// <summary>Bilgisayar adına göre iptal edilmemiş cihaz.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="machineName">Bilgisayar adı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz.</returns>
    public Task<AgentDevice?> FindActiveByMachineAsync(Guid userId, string machineName, CancellationToken cancellationToken = default)
    {
        var name = (machineName ?? string.Empty).Trim();
        return Query()
            .Where(d => d.UserId == userId && !d.IsRevoked && d.MachineName == name)
            .OrderByDescending(d => d.PairedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Token ailesine göre iptal edilmemiş cihaz.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenFamilyId">Aile.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz.</returns>
    public Task<AgentDevice?> FindActiveByTokenFamilyAsync(Guid userId, Guid tokenFamilyId, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(d => d.UserId == userId && !d.IsRevoked && d.TokenFamilyId == tokenFamilyId, cancellationToken);
}
