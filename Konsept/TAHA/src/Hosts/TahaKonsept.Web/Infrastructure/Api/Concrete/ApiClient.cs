using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TahaKonsept.Web.Infrastructure.Api.Base;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;
using TahaKonsept.Web.Infrastructure.Options;

namespace TahaKonsept.Web.Infrastructure.Api.Concrete;

/// <summary>
/// Varsayılan API istemcisi. Typed HttpClient olarak kaydedilir (BaseAddress = <see cref="ApiOptions.BaseUrl"/>);
/// tüm akış <see cref="ApiClientBase"/>'dedir.
/// </summary>
public sealed class ApiClient : ApiClientBase
{
    private readonly TimeSpan _refreshSkew;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="http">API HttpClient'ı.</param>
    /// <param name="tokens">Token deposu.</param>
    /// <param name="refresher">Token yenileyici.</param>
    /// <param name="clock">Saat kaynağı.</param>
    /// <param name="options">API ayarları.</param>
    /// <param name="logger">Günlükleyici.</param>
    public ApiClient(HttpClient http, ITokenStore tokens, ITokenRefresher refresher, TimeProvider clock, IOptions<ApiOptions> options, ILogger<ApiClient> logger)
        : base(http, tokens, refresher, clock, logger)
    {
        _refreshSkew = TimeSpan.FromSeconds(Math.Max(0, options.Value.RefreshSkewSeconds));
    }

    /// <inheritdoc />
    protected override TimeSpan RefreshSkew => _refreshSkew;
}
