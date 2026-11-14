using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Auth.Base;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Http.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Agent.Core.Auth.Concrete;

/// <summary>
/// Ajan oturum servisi: yenilemeyi <c>POST api/identity/refresh</c> ile yapar, reddedilirse oturumu kapatır
/// (web'den çıkış → ajan da yetkisiz kalır) ve loglar.
/// </summary>
public sealed class AgentAuthService : AgentAuthServiceBase
{
    private readonly IAnonymousAgentApiClient _api;
    private readonly ILogger<AgentAuthService> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="store">Token deposu.</param>
    /// <param name="api">Anonim API istemcisi.</param>
    /// <param name="options">Seçenekler.</param>
    /// <param name="timeProvider">Zaman sağlayıcı.</param>
    /// <param name="logger">Logger.</param>
    public AgentAuthService(ITokenStore store, IAnonymousAgentApiClient api, AgentOptions options, TimeProvider timeProvider, ILogger<AgentAuthService> logger)
        : base(store, options.TokenRefreshSkew, timeProvider)
    {
        _api = api;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override Task<Result<TokenResponse>> RequestRefreshAsync(string refreshToken, CancellationToken ct) =>
        _api.RefreshAsync(new AgentRefreshRequest(refreshToken), ct);

    /// <inheritdoc />
    protected override void OnRefreshRejected(Error error) =>
        _logger.LogWarning("Token yenileme reddedildi ({Code}); ajan yeniden eşleme bekliyor.", error.Code);

    /// <inheritdoc />
    protected override void OnRefreshFailed(Error error) =>
        _logger.LogWarning("Token yenilenemedi ({Code}): {Message}", error.Code, error.Message);
}
