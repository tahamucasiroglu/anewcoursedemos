using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TahaKonsept.Agent.Engines.Image.Downloads.Base;
using TahaKonsept.Agent.Engines.Image.Options;

namespace TahaKonsept.Agent.Engines.Image.Downloads.Concrete;

/// <summary>
/// <c>IHttpClientFactory</c> tabanlı indirici. Hugging Face adreslerine, ayarlıysa <see cref="ImageEngineOptions.HuggingFaceToken"/>
/// ile <c>Authorization: Bearer</c> ekler (kapılı depolar için).
/// </summary>
public sealed class HttpModelFileDownloader : ModelFileDownloaderBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ImageEngineOptions _options;

    /// <summary>İndiriciyi oluşturur.</summary>
    /// <param name="httpClientFactory">HTTP istemci fabrikası.</param>
    /// <param name="options">Görsel motor ayarları.</param>
    /// <param name="logger">Günlükleyici.</param>
    public HttpModelFileDownloader(IHttpClientFactory httpClientFactory, IOptions<ImageEngineOptions> options, ILogger<HttpModelFileDownloader> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override int RetryCount => _options.DownloadRetryCount;

    /// <inheritdoc />
    protected override HttpClient CreateClient() => _httpClientFactory.CreateClient(ImageEngineOptions.HttpClientName);

    /// <inheritdoc />
    protected override void PrepareRequest(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.HuggingFaceToken)
            && request.RequestUri is { } uri
            && uri.Host.EndsWith("huggingface.co", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.HuggingFaceToken);
        }
    }
}
