using System.Globalization;
using System.Text;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Core.Media.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Media.Base;

/// <summary>
/// FFmpeg/ffprobe komutlarını <see cref="IProcessRunner"/> ile çalıştıran medya yardımcı tabanı. Araçlar <see cref="IToolLocator"/>'dan çözülür;
/// yoksa anlaşılır hata döner. Zaman aşımı <see cref="CommandTimeout"/>.
/// </summary>
public abstract class MediaProbeBase : IMediaProbe
{
    private readonly IToolLocator _tools;
    private readonly IProcessRunner _processes;

    /// <summary>Oluşturur.</summary>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="processes">Süreç çalıştırıcı.</param>
    protected MediaProbeBase(IToolLocator tools, IProcessRunner processes)
    {
        _tools = tools;
        _processes = processes;
    }

    /// <summary>Komut zaman aşımı.</summary>
    protected virtual TimeSpan CommandTimeout => TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public async Task<Result<double>> GetDurationAsync(string path, CancellationToken ct)
    {
        var ffprobe = _tools.Require(ToolKind.Ffprobe);
        if (ffprobe.IsFailure)
        {
            return ffprobe.Error;
        }

        var result = await _processes.RunAsync(
            new ProcessSpec(ffprobe.Value, ["-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", path], Timeout: TimeSpan.FromMinutes(1)),
            null,
            ct);
        if (!result.Succeeded)
        {
            return Error.Failure("agent.ffprobe_failed", "Medya süresi okunamadı: " + Tail(result.StandardError));
        }

        return double.TryParse(result.StandardOutput.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? seconds
            : Error.Failure("agent.ffprobe_failed", "Medya süresi okunamadı.");
    }

    /// <inheritdoc />
    public Task<Result> ExtractThumbnailAsync(string videoPath, string outputPath, double atSec, CancellationToken ct) =>
        RunFfmpegAsync(
            ["-y", "-ss", atSec.ToString("0.###", CultureInfo.InvariantCulture), "-i", videoPath, "-frames:v", "1", "-vf", "scale=480:-2", "-q:v", "3", outputPath],
            "agent.thumbnail_failed",
            "Küçük resim üretilemedi",
            ct);

    /// <inheritdoc />
    public Task<Result> ConcatAudioAsync(IReadOnlyList<string> inputs, string outputPath, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var arguments = new List<string> { "-y" };
        var filter = new StringBuilder();
        for (var i = 0; i < inputs.Count; i++)
        {
            arguments.Add("-i");
            arguments.Add(inputs[i]);
            filter.Append('[').Append(i.ToString(CultureInfo.InvariantCulture)).Append(":a]");
        }

        filter.Append("concat=n=").Append(inputs.Count.ToString(CultureInfo.InvariantCulture)).Append(":v=0:a=1[out]");
        arguments.AddRange(["-filter_complex", filter.ToString(), "-map", "[out]", "-ac", "1", "-ar", "24000", "-c:a", "pcm_s16le", outputPath]);
        return RunFfmpegAsync(arguments, "agent.audio_concat_failed", "Ses dosyaları birleştirilemedi", ct);
    }

    /// <summary>FFmpeg'i çalıştırır.</summary>
    /// <param name="arguments">Argümanlar.</param>
    /// <param name="errorCode">Hata kodu.</param>
    /// <param name="errorMessage">Hata mesajı öneki.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected async Task<Result> RunFfmpegAsync(IReadOnlyList<string> arguments, string errorCode, string errorMessage, CancellationToken ct)
    {
        var ffmpeg = _tools.Require(ToolKind.Ffmpeg);
        if (ffmpeg.IsFailure)
        {
            return ffmpeg.Error;
        }

        var result = await _processes.RunAsync(new ProcessSpec(ffmpeg.Value, arguments, Timeout: CommandTimeout), null, ct);
        return result.Succeeded
            ? Result.Success()
            : Error.Failure(errorCode, errorMessage + ": " + Tail(result.StandardError));
    }

    /// <summary>Uzun çıktının son kısmını döner.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Son 800 karakter.</returns>
    protected static string Tail(string text) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Length <= 800 ? text.Trim() : text[^800..].Trim();
}
