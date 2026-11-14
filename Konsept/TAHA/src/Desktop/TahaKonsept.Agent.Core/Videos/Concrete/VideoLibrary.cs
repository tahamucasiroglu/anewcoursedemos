using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Shell.Abstract;
using TahaKonsept.Agent.Core.Videos.Abstract;
using TahaKonsept.Agent.Core.Videos.Base;

namespace TahaKonsept.Agent.Core.Videos.Concrete;

/// <summary>Loglayan yerel video kütüphanesi.</summary>
public sealed class VideoLibrary : VideoLibraryBase
{
    private readonly ILogger<VideoLibrary> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="index">Video dizini.</param>
    /// <param name="shell">Kabuk başlatıcı.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="logger">Logger.</param>
    public VideoLibrary(IVideoIndex index, IShellLauncher shell, IAgentPaths paths, ILogger<VideoLibrary> logger)
        : base(index, shell, paths)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override void OnDeleted(VideoEntry entry, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
        {
            _logger.LogInformation("Video silindi: {VideoId}", entry.VideoId);
        }
        else
        {
            _logger.LogWarning("Video silindi ama bazı dosyalar kaldı: {VideoId} ({Count})", entry.VideoId, failures.Count);
        }
    }
}
