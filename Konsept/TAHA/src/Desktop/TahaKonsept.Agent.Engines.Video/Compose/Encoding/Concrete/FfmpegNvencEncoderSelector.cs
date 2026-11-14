using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Engines.Video.Compose.Encoding.Base;

namespace TahaKonsept.Agent.Engines.Video.Compose.Encoding.Concrete;

/// <summary>
/// ffmpeg ile NVENC yoklaması: (1) <c>ffmpeg -hide_banner -encoders</c> çıktısında <c>h264_nvenc</c> var mı (derleme desteği),
/// (2) 256×144, 3 karelik deneme kodlaması (<see cref="BuildProbeArguments"/>; NVIDIA GPU + sürücü + boş NVENC oturumu gerçekten var mı).
/// İkisi de geçerse NVENC seçilir; aksi hâlde neden (stderr son satırları) döner ve taban sınıf libx264'e düşüp uyarı loglar.
/// </summary>
public sealed class FfmpegNvencEncoderSelector : VideoEncoderSelectorBase
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(30);

    private readonly IProcessRunner _runner;

    /// <summary>Seçici oluşturur.</summary>
    /// <param name="runner">Süreç çalıştırıcı.</param>
    /// <param name="logger">Günlükçü.</param>
    public FfmpegNvencEncoderSelector(IProcessRunner runner, ILogger<FfmpegNvencEncoderSelector> logger)
        : base(logger)
    {
        _runner = runner;
    }

    /// <summary>Deneme kodlaması argümanları (sesli değil, çıktı <c>null</c> muxer'a).</summary>
    /// <returns>ffmpeg argümanları.</returns>
    public static IReadOnlyList<string> BuildProbeArguments() =>
    [
        "-hide_banner", "-loglevel", "error", "-nostats",
        "-f", "lavfi", "-i", "color=c=black:s=256x144:r=25",
        "-frames:v", "3", "-c:v", VideoEncoderChoice.NvencEncoder, "-pix_fmt", "yuv420p",
        "-f", "null", "-",
    ];

    /// <inheritdoc />
    protected override async Task<string?> ProbeGpuEncoderAsync(string ffmpegPath, CancellationToken ct)
    {
        var list = await _runner.RunAsync(new ProcessSpec(ffmpegPath, ["-hide_banner", "-encoders"], Timeout: ProbeTimeout), null, ct).ConfigureAwait(false);
        if (!list.Succeeded)
        {
            return $"ffmpeg kodlayıcı listesi okunamadı (kod {list.ExitCode})";
        }

        if (!list.StandardOutput.Contains(VideoEncoderChoice.NvencEncoder, StringComparison.Ordinal))
        {
            return "ffmpeg derlemesinde h264_nvenc yok";
        }

        var trial = await _runner.RunAsync(new ProcessSpec(ffmpegPath, BuildProbeArguments(), Timeout: ProbeTimeout), null, ct).ConfigureAwait(false);
        if (trial.Succeeded)
        {
            return null;
        }

        var err = trial.TimedOut ? "zaman aşımı" : trial.StandardError.Trim();
        err = err.Length <= 400 ? err : err[^400..];
        return $"NVENC deneme kodlaması başarısız (NVIDIA GPU/sürücü yok ya da NVENC meşgul): {err}";
    }
}
