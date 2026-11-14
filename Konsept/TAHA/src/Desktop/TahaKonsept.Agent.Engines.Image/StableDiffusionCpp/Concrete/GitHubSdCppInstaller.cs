using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TahaKonsept.Agent.Abstractions.Gpu.Abstract;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Engines.Image.Downloads.Abstract;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Concrete;

/// <summary>Sürüm bilgisini GitHub releases API'den (<see cref="ImageEngineOptions.SdCppReleaseApiUrl"/>) alan sd.cpp kurucusu.</summary>
public sealed class GitHubSdCppInstaller : SdCppInstallerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Kurucuyu oluşturur.</summary>
    /// <param name="httpClientFactory">HTTP istemci fabrikası.</param>
    /// <param name="gpu">GPU yoklaması.</param>
    /// <param name="downloader">Dosya indirici.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="options">Ayarlar.</param>
    /// <param name="logger">Günlükleyici.</param>
    /// <param name="toolLocator">İsteğe bağlı araç bulucu.</param>
    public GitHubSdCppInstaller(
        IHttpClientFactory httpClientFactory,
        IGpuCapabilityProbe gpu,
        IModelFileDownloader downloader,
        IAgentPaths paths,
        IOptions<ImageEngineOptions> options,
        ILogger<GitHubSdCppInstaller> logger,
        IToolLocator? toolLocator = null)
        : base(gpu, downloader, paths, options.Value, logger, toolLocator)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    protected override async Task<Result<GitHubRelease>> GetLatestReleaseAsync(CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Options.SdCppReleaseApiUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            using var response = await _httpClientFactory.CreateClient(ImageEngineOptions.HttpClientName)
                .SendAsync(request, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Error.Failure("sdcpp.release.http", $"stable-diffusion.cpp sürüm bilgisi alınamadı (HTTP {(int)response.StatusCode}).");
            }

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>(timeout.Token).ConfigureAwait(false);
            return release is null
                ? Error.Failure("sdcpp.release.empty", "stable-diffusion.cpp sürüm bilgisi boş geldi.")
                : release;
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            Logger.LogWarning(ex, "sd.cpp sürüm bilgisi alınamadı");
            return Error.Failure("sdcpp.release.network", $"GitHub'a ulaşılamadı: {ex.Message}");
        }
    }
}
