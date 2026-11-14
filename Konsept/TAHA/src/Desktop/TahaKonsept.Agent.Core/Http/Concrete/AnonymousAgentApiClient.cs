using TahaKonsept.Agent.Core.Auth;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Http.Abstract;
using TahaKonsept.Agent.Core.Http.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Pairing;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Agent.Core.Http.Concrete;

/// <summary>Kimlik doğrulamasız ajan API istemcisi (eşleme ve yenileme uçları).</summary>
public sealed class AnonymousAgentApiClient : AgentApiClientBase, IAnonymousAgentApiClient
{
    /// <summary>Oluşturur.</summary>
    /// <param name="httpClient">HttpClient.</param>
    /// <param name="settings">Ayar deposu.</param>
    public AnonymousAgentApiClient(HttpClient httpClient, IAgentSettingsStore settings)
        : base(httpClient, settings)
    {
    }

    /// <inheritdoc />
    public Task<Result<DeviceStartResponse>> StartDeviceAsync(DeviceStartRequest request, CancellationToken ct) =>
        PostAsync<DeviceStartRequest, DeviceStartResponse>(AgentApiRoutes.DeviceStart, request, ct);

    /// <inheritdoc />
    public Task<Result<TokenResponse>> RequestDeviceTokenAsync(DeviceTokenRequest request, CancellationToken ct) =>
        PostAsync<DeviceTokenRequest, TokenResponse>(AgentApiRoutes.DeviceToken, request, ct);

    /// <inheritdoc />
    public Task<Result<TokenResponse>> RefreshAsync(AgentRefreshRequest request, CancellationToken ct) =>
        PostAsync<AgentRefreshRequest, TokenResponse>(AgentApiRoutes.Refresh, request, ct);
}
