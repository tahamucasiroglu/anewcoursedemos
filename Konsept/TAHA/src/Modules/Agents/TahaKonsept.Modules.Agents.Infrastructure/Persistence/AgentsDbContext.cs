using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Agents.Domain.Entities;

namespace TahaKonsept.Modules.Agents.Infrastructure.Persistence;

/// <summary>
/// Agents modülünün DbContext'i (şema <c>agents</c>): <see cref="AgentDevices"/>, <see cref="DevicePairings"/>, <see cref="StoredArtifacts"/>.
/// Yapılandırmalar <c>Persistence/Configurations</c> altında, migration'lar <c>Persistence/Migrations</c> altında.
/// </summary>
public sealed class AgentsDbContext : ModuleDbContextBase
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "agents";

    /// <summary>Oluşturur.</summary>
    /// <param name="options">EF seçenekleri.</param>
    /// <param name="clock">Saat.</param>
    public AgentsDbContext(DbContextOptions<AgentsDbContext> options, IClock clock)
        : base(options, clock)
    {
    }

    /// <summary>Şema.</summary>
    public override string Schema => SchemaName;

    /// <summary>Eşlenmiş cihazlar.</summary>
    public DbSet<AgentDevice> AgentDevices => Set<AgentDevice>();

    /// <summary>Eşleme istekleri.</summary>
    public DbSet<DevicePairing> DevicePairings => Set<DevicePairing>();

    /// <summary>Geçici artefaktlar.</summary>
    public DbSet<StoredArtifact> StoredArtifacts => Set<StoredArtifact>();
}
