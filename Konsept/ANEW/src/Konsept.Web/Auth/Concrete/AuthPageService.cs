using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Auth.Abstract;
using Konsept.Web.Auth.Base;

namespace Konsept.Web.Auth.Concrete;

/// <summary>Giriş/kayıt/çıkış sayfa servisi; Api'ye typed HTTP istemcisiyle konuşur.</summary>
/// <param name="api">API istemcisi.</param>
/// <param name="logger">Teknik log.</param>
/// <param name="coordinator">Refresh koordinatörü.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class AuthPageService(IKonseptApiClient api, ILogger<AuthPageService> logger, ITokenRefreshCoordinator coordinator, TimeProvider timeProvider)
    : AuthPageServiceBase
{
    /// <inheritdoc />
    protected override IKonseptApiClient Api => api;

    /// <inheritdoc />
    protected override ILogger Logger => logger;

    /// <inheritdoc />
    protected override ITokenRefreshCoordinator Coordinator => coordinator;

    /// <inheritdoc />
    protected override TimeProvider Time => timeProvider;
}
