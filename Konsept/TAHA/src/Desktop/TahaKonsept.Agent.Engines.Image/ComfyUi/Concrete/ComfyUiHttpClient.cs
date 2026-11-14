using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TahaKonsept.Agent.Engines.Image.ComfyUi.Base;
using TahaKonsept.Agent.Engines.Image.Options;

namespace TahaKonsept.Agent.Engines.Image.ComfyUi.Concrete;

/// <summary><see cref="ImageEngineOptions.ComfyUiBaseUrl"/> adresindeki ComfyUI'ye <c>IHttpClientFactory</c> ile bağlanan istemci.</summary>
public sealed class ComfyUiHttpClient : ComfyUiClientBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>İstemciyi oluşturur.</summary>
    /// <param name="httpClientFactory">HTTP istemci fabrikası.</param>
    /// <param name="options">Ayarlar.</param>
    /// <param name="logger">Günlükleyici.</param>
    public ComfyUiHttpClient(IHttpClientFactory httpClientFactory, IOptions<ImageEngineOptions> options, ILogger<ComfyUiHttpClient> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        BaseUri = new Uri(options.Value.ComfyUiBaseUrl.TrimEnd('/') + "/");
    }

    /// <inheritdoc />
    protected override Uri BaseUri { get; }

    /// <inheritdoc />
    protected override HttpClient CreateClient() => _httpClientFactory.CreateClient(ImageEngineOptions.HttpClientName);
}
