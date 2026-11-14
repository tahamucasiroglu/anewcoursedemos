using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Engines.Video.Compose.Encoding.Abstract;

namespace TahaKonsept.Agent.Engines.Video.Compose.Encoding.Base;

/// <summary>
/// <see cref="IVideoEncoderSelector"/> ortak davranışı (GPU'suz test edilebilir): ffmpeg yolu başına tek yoklama (önbellekli görev),
/// <see cref="ProbeGpuEncoderAsync"/> <c>null</c> dönerse NVENC, neden dönerse uyarı loglanıp yazılım kodlayıcı.
/// <see cref="ReportGpuFailure"/> sonrası (çalışma sırasında NVENC hatası) süreç boyunca yazılım kodlayıcı döner.
/// Somut sınıf yalnızca yoklamanın nasıl yapılacağını bilir.
/// </summary>
public abstract class VideoEncoderSelectorBase : IVideoEncoderSelector
{
    private readonly ConcurrentDictionary<string, Lazy<Task<VideoEncoderChoice>>> _probes = new(StringComparer.OrdinalIgnoreCase);
    private volatile VideoEncoderChoice? _disabled;

    /// <summary>Günlükçü ile kurar.</summary>
    /// <param name="logger">Günlükçü.</param>
    protected VideoEncoderSelectorBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>Günlükçü.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public Task<VideoEncoderChoice> SelectAsync(string ffmpegPath, CancellationToken ct)
    {
        if (_disabled is { } disabled)
        {
            return Task.FromResult(disabled);
        }

        // Yoklama paylaşılan görevdir: çağıranın iptali yoklamayı yarıda bırakıp önbelleğe yanlış sonuç yazmasın.
        var probe = _probes.GetOrAdd(ffmpegPath, path => new Lazy<Task<VideoEncoderChoice>>(() => ProbeAndDecideAsync(path)));
        return probe.Value.WaitAsync(ct);
    }

    /// <inheritdoc />
    public VideoEncoderChoice ReportGpuFailure(string reason)
    {
        var fallback = VideoEncoderChoice.SoftwareFallback(reason);
        _disabled = fallback;
        Logger.LogWarning("Video kodlama: NVENC çalışırken başarısız oldu, bu oturumda libx264 (CPU) kullanılacak — {Reason}", reason);
        return fallback;
    }

    /// <summary>GPU kodlayıcıyı yoklar.</summary>
    /// <param name="ffmpegPath">ffmpeg yolu.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kullanılabilirse <c>null</c>, değilse nedeni.</returns>
    protected abstract Task<string?> ProbeGpuEncoderAsync(string ffmpegPath, CancellationToken ct);

    private async Task<VideoEncoderChoice> ProbeAndDecideAsync(string ffmpegPath)
    {
        string? reason;
        try
        {
            reason = await ProbeGpuEncoderAsync(ffmpegPath, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception or TimeoutException)
        {
            reason = $"NVENC yoklaması hata verdi ({ex.GetType().Name}: {ex.Message})";
        }

        if (reason is null)
        {
            Logger.LogInformation("Video kodlama: GPU kodlayıcı {Encoder} kullanılacak", VideoEncoderChoice.NvencEncoder);
            return VideoEncoderChoice.Nvenc;
        }

        Logger.LogWarning("Video kodlama: GPU kodlayıcı kullanılamıyor, libx264 (CPU) kullanılacak — {Reason}", reason);
        return VideoEncoderChoice.SoftwareFallback(reason);
    }
}
