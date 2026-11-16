namespace Konsept.Api.Tests.Fakes;

/// <summary>Her istemciyi verilen sahte işleyiciye bağlayan HTTP istemci fabrikası.</summary>
/// <param name="handler">Sahte işleyici.</param>
public sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    /// <inheritdoc />
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
