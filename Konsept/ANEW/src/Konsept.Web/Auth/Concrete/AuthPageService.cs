using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Auth.Base;

namespace Konsept.Web.Auth.Concrete;

/// <summary>Giriş/kayıt/çıkış sayfa servisi; Api'ye typed HTTP istemcisiyle konuşur.</summary>
/// <param name="api">API istemcisi.</param>
/// <param name="logger">Teknik log.</param>
public sealed class AuthPageService(IKonseptApiClient api, ILogger<AuthPageService> logger) : AuthPageServiceBase
{
    /// <inheritdoc />
    protected override IKonseptApiClient Api => api;

    /// <inheritdoc />
    protected override ILogger Logger => logger;
}
