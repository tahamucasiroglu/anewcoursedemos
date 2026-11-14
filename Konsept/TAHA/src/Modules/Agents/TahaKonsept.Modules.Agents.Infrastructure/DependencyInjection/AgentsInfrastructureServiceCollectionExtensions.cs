using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Concrete;
using TahaKonsept.Modules.Agents.Application.Options;
using TahaKonsept.Modules.Agents.Application.Validators;
using TahaKonsept.Modules.Agents.Infrastructure.Logging.Concrete;
using TahaKonsept.Modules.Agents.Infrastructure.Persistence;
using TahaKonsept.Modules.Agents.Infrastructure.Persistence.Concrete;
using TahaKonsept.Modules.Agents.Infrastructure.Security.Concrete;
using TahaKonsept.Modules.Agents.Infrastructure.Storage.Concrete;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Modules.Agents.Infrastructure.DependencyInjection;

/// <summary>
/// Agents modülü servis kaydı (Api katmanındaki <c>IAgentClientProvider</c> ve <c>IVerificationUrlProvider</c> hariç).
/// Singleton: bağlantı izleyici, önizleme eşleyici, adres/kod üreticileri, <see cref="AgentsOptions"/>. Scoped: DbContext, depolar, servisler,
/// <see cref="IAgentGateway"/>, <see cref="IArtifactStore"/>. Uzatma noktaları TryAdd ile (önce kaydeden kazanır):
/// <see cref="IAgentStageEventHandler"/> (no-op), <see cref="IAgentLogSink"/> (Serilog + Diagnostics işlem günlüğü). Identity köprüleri: <see cref="IAgentTokenIssuer"/>,
/// <see cref="ISessionRevocationNotifier"/> (enumerable).
/// </summary>
public static class AgentsInfrastructureServiceCollectionExtensions
{
    /// <summary>Agents servislerini ekler.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddAgentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(AgentsOptions.SectionName).Get<AgentsOptions>() ?? new AgentsOptions();
        services.TryAddSingleton(options);

        services.AddModuleDbContext<AgentsDbContext>(configuration, AgentsDbContext.SchemaName);
        services.AddScoped<IAgentDeviceRepository, AgentDeviceRepository>();
        services.AddScoped<IDevicePairingRepository, DevicePairingRepository>();
        services.AddScoped<IStoredArtifactRepository, StoredArtifactRepository>();

        services.TryAddSingleton<IAgentConnectionTracker, InMemoryAgentConnectionTracker>();
        services.TryAddSingleton<ISpeechPreviewCorrelator, SpeechPreviewCorrelator>();
        services.TryAddSingleton<IAgentVideoUrlBuilder, AgentVideoUrlBuilder>();
        services.TryAddSingleton<IPairingCodeGenerator, RandomPairingCodeGenerator>();

        services.AddScoped<IDevicePairingService, DevicePairingService>();
        services.AddScoped<IAgentDeviceService, AgentDeviceService>();
        services.AddScoped<IAgentHubService, AgentHubService>();
        services.AddScoped<IAgentGateway, AgentGateway>();
        services.AddScoped<IAgentSessionRevoker, AgentSessionRevoker>();
        services.AddScoped<IAgentArtifactService, AgentArtifactService>();
        services.AddScoped<IArtifactStore, FileSystemArtifactStore>();

        services.AddScoped<IAgentTokenIssuer, IdentityAgentTokenIssuer>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISessionRevocationNotifier, AgentSessionRevocationNotifier>());

        services.TryAddScoped<IAgentStageEventHandler, NullAgentStageEventHandler>();
        services.TryAddScoped<LoggingAgentLogSink>();
        services.TryAddScoped<IAgentLogSink, ActivityAgentLogSink>();

        services.AddScoped<IValidator<DeviceStartRequest>, DeviceStartRequestValidator>();
        services.AddScoped<IValidator<DeviceTokenRequest>, DeviceTokenRequestValidator>();
        services.AddScoped<IValidator<DeviceApproveRequest>, DeviceApproveRequestValidator>();
        return services;
    }
}
