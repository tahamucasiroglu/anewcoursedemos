using Konsept.Web.ApiClient.Base;

namespace Konsept.Web.ApiClient.Concrete;

/// <summary>
/// API'ye <see cref="HttpClient"/> ile konuşan istemci (taban adres ayardan: <c>Api:BaseUrl</c>).
/// </summary>
public sealed class KonseptApiClient : KonseptApiClientBase
{
    private readonly HttpClient _http;

    /// <summary>Initializes a new instance of the <see cref="KonseptApiClient"/> class.</summary>
    /// <param name="http">Taban adresi ayarlanmış HTTP istemcisi.</param>
    public KonseptApiClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> TransmitAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        _http.SendAsync(request, cancellationToken);
}
