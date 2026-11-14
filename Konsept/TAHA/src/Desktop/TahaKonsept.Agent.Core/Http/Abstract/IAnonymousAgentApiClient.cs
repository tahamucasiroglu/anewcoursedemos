using TahaKonsept.Agent.Core.Auth;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Pairing;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Agent.Core.Http.Abstract;

/// <summary>
/// Kimlik doğrulamasız API çağrıları: cihaz eşleme başlatma/yoklama ve token yenileme.
/// </summary>
public interface IAnonymousAgentApiClient
{
    /// <summary><c>POST api/agents/device/start</c>.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Cihaz/kullanıcı kodu.</returns>
    Task<Result<DeviceStartResponse>> StartDeviceAsync(DeviceStartRequest request, CancellationToken ct);

    /// <summary><c>POST api/agents/device/token</c>; onay bekleniyorsa <c>agents.device.authorization_pending</c> kodlu hata.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Token çifti ya da hata.</returns>
    Task<Result<TokenResponse>> RequestDeviceTokenAsync(DeviceTokenRequest request, CancellationToken ct);

    /// <summary><c>POST api/identity/refresh</c>.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yeni token çifti ya da hata.</returns>
    Task<Result<TokenResponse>> RefreshAsync(AgentRefreshRequest request, CancellationToken ct);
}
