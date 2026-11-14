namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Hep aynı işleyiciyle istemci üreten sahte <see cref="IHttpClientFactory"/>.</summary>
public sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    /// <summary>Fabrikayı oluşturur.</summary>
    /// <param name="handler">İşleyici.</param>
    public StubHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}
