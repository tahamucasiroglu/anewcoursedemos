using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.SpeechPreview.Abstract;
using TahaKonsept.Agent.Core.VideoServer.Base;
using TahaKonsept.Agent.Core.Videos.Abstract;

namespace TahaKonsept.Agent.Core.VideoServer.Concrete;

/// <summary>
/// Yerel video sunucusu: port ve akış anahtarı <c>AgentSettings</c>'ten, dosyalar <see cref="IVideoIndex"/> ve
/// <see cref="ISpeechPreviewService"/>'ten çözülür.
/// </summary>
public sealed class LocalVideoServer : VideoServerBase
{
    private readonly IAgentSettingsStore _settings;
    private readonly IVideoIndex _index;
    private readonly ISpeechPreviewService _previews;

    /// <summary>Oluşturur.</summary>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="index">Video dizini.</param>
    /// <param name="previews">Ses önizleme servisi.</param>
    /// <param name="logger">Logger.</param>
    public LocalVideoServer(IAgentSettingsStore settings, IVideoIndex index, ISpeechPreviewService previews, ILogger<LocalVideoServer> logger)
        : base(logger)
    {
        _settings = settings;
        _index = index;
        _previews = previews;
    }

    /// <inheritdoc />
    protected override int GetPort() => _settings.Current.VideoServerPort;

    /// <inheritdoc />
    protected override string GetStreamKey() => _settings.Current.StreamKey;

    /// <inheritdoc />
    protected override string? ResolveVideo(Guid videoId) => _index.Find(videoId)?.VideoPath;

    /// <inheritdoc />
    protected override string? ResolveThumbnail(Guid videoId) => _index.Find(videoId)?.ThumbnailPath;

    /// <inheritdoc />
    protected override string? ResolvePreview(string fileName) => _previews.ResolvePreviewFile(fileName);
}
