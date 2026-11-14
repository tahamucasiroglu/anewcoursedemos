using TahaKonsept.Agent.Core.Downloads.Base;
using TahaKonsept.Agent.Core.Http;

namespace TahaKonsept.Agent.Core.Downloads.Concrete;

/// <summary><c>IHttpClientFactory</c>'nin <see cref="AgentHttpClientNames.Downloads"/> istemcisiyle indiren sınıf.</summary>
public sealed class HttpFileDownloader : FileDownloaderBase
{
    private readonly IHttpClientFactory _factory;

    /// <summary>Oluşturur.</summary>
    /// <param name="factory">HTTP istemci fabrikası.</param>
    public HttpFileDownloader(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    protected override HttpClient CreateClient() => _factory.CreateClient(AgentHttpClientNames.Downloads);
}
