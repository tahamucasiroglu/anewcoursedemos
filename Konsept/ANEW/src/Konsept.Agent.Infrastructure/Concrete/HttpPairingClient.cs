using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Infrastructure.Base;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>Eşleştirme uçlarını ayarlardaki API adresine HTTP ile çağırır (10 sn zaman aşımı).</summary>
public sealed class HttpPairingClient : PairingClientBase, IDisposable
{
    private readonly HttpClient _http;

    /// <summary>Initializes a new instance of the <see cref="HttpPairingClient"/> class.</summary>
    /// <param name="settings">Ajan ayarları (API adresi).</param>
    /// <param name="handlerFactory">Testte HTTP yolunu değiştirmek için; normalde <c>null</c>.</param>
    public HttpPairingClient(AgentSettings settings, Func<HttpMessageHandler>? handlerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _http = handlerFactory is null ? new HttpClient() : new HttpClient(handlerFactory(), disposeHandler: true);
        _http.BaseAddress = settings.ServerUrl;
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        _http.SendAsync(request, cancellationToken);
}
