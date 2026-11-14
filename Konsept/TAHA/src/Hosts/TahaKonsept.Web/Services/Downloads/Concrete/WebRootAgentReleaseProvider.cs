using TahaKonsept.Web.Services.Downloads.Base;

namespace TahaKonsept.Web.Services.Downloads.Concrete;

/// <summary>
/// Kurulum dosyasını sitenin statik kökündeki <c>wwwroot/downloads/</c> klasöründen okuyan sağlayıcı
/// (deploy/publish-pi.ps1 bu klasörü Pi'ye taşır; statik dosya ara katmanı aynı yerden sunar).
/// </summary>
public sealed class WebRootAgentReleaseProvider : AgentReleaseProviderBase
{
    private readonly IWebHostEnvironment _environment;

    /// <summary>Ortamı ve günlükçüyü alır.</summary>
    /// <param name="environment">Web ortamı (WebRootPath).</param>
    /// <param name="logger">Günlükçü.</param>
    public WebRootAgentReleaseProvider(IWebHostEnvironment environment, ILogger<WebRootAgentReleaseProvider> logger)
        : base(logger) => _environment = environment;

    /// <inheritdoc />
    protected override string DownloadsDirectory =>
        Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "downloads");
}
