using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.BuildingBlocks.Api.Modules.Base;
using TahaKonsept.Modules.Agents.Api.Hubs;
using TahaKonsept.Modules.Agents.Api.Hubs.Concrete;
using TahaKonsept.Modules.Agents.Api.RateLimiting;
using TahaKonsept.Modules.Agents.Api.Security.Concrete;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Infrastructure.DependencyInjection;
using TahaKonsept.Modules.Agents.Infrastructure.Persistence;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Api;

/// <summary>
/// Agents modülü kaydı: altyapı/uygulama servisleri (<c>AddAgentsModule</c>), SignalR istemci vekili, doğrulama adresi sağlayıcı,
/// anonim eşleme uçlarının IP başına istek sınırları (<see cref="AgentsRateLimitPolicies"/>; host <c>UseRateLimiter</c> çağırır), hub eşlemesi (<c>/hubs/agent</c>) ve <see cref="AgentsDbContext"/> migration'ı. ApiModules listesinde Identity ve Production'dan sonra gelmelidir
/// (onların <c>IAgentStageEventHandler</c>/<c>IAgentLogSink</c> kayıtları TryAdd varsayılanlarından önce yapılsın).
/// </summary>
public sealed class AgentsModule : ModuleBase
{
    /// <summary>Modül adı.</summary>
    public override string Name => "Agents";

    /// <summary>Servisleri kaydeder.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddAgentsModule(configuration);
        services.TryAddSingleton<IAgentClientProvider, SignalRAgentClientProvider>();
        services.TryAddScoped<IVerificationUrlProvider, HttpVerificationUrlProvider>();
        services.AddRateLimiter(AgentsRateLimitPolicies.Register);
    }

    /// <summary>Migration'ları uygular.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        MigrateDbContextAsync<AgentsDbContext>(services, cancellationToken);

    /// <summary>Hub'ı eşler.</summary>
    /// <param name="endpoints">Uç oluşturucu.</param>
    public override void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapHub<AgentHub>(AgentHubRoutes.HubPath);
}
