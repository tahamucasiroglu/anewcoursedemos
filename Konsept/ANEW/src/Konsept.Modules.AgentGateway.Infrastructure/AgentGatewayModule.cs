using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Concrete;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Infrastructure.Concrete;
using Konsept.Modules.AgentGateway.Infrastructure.Hubs;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Konsept.Modules.AgentGateway.Infrastructure;

/// <summary>
/// AgentGateway modülünün servis kaydı ve hub uç noktası.
/// </summary>
public static class AgentGatewayModule
{
    /// <summary>Modülün servislerini kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddAgentGatewayModule(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IAgentConnectionRegistry, InMemoryAgentConnectionRegistry>();
        services.AddSingleton<IAgentConnectionTracker, AgentConnectionTracker>();
        services.AddSingleton<IAgentDeviceRevocationListener, AgentSessionTerminator>();
        services.AddSingleton<IAgentChannel, SignalRAgentChannel>();
        services.AddScoped<IAgentPresenceService, AgentPresenceService>();
        services.AddScoped<IAgentStatusReader>(sp => sp.GetRequiredService<IAgentPresenceService>());
        services.AddScoped<IAgentTestService, AgentTestService>();
        services.AddSingleton<AgentLogQuota>();
        services.AddScoped<IAgentLogService, AgentLogService>();
        return services;
    }

    /// <summary>
    /// Ajan hub'ını <see cref="AgentHubContract.HubPath"/> yoluna bağlar. Yalnızca geçerli ajan oturumu bağlanabilir
    /// (<see cref="AuthPolicies.AgentHub"/>, spec 0012 Req 10 — 0002'nin geçici kimliksiz istisnası kaldırıldı).
    /// </summary>
    /// <param name="endpoints">Uç nokta kurucusu.</param>
    /// <returns>Aynı kurucu.</returns>
    public static IEndpointRouteBuilder MapAgentGateway(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<AgentHub>(AgentHubContract.HubPath).RequireAuthorization(AuthPolicies.AgentHub);
        return endpoints;
    }
}
