using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Compose;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Engines.Video.Compose.Base;
using TahaKonsept.Agent.Engines.Video.Media.Abstract;
using TahaKonsept.Agent.Engines.Video.Subtitles;
using TahaKonsept.Agent.Engines.Video.Subtitles.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Compose.Concrete;

/// <summary>
/// ffmpeg ile son video: (1) her sahne hedef boyut/fps'e normalize edilir (scale + pad, sessiz), hedef süresinden kısaysa son kare
/// <c>tpad=stop_mode=clone</c> ile uzatılır, uzunsa kesilir; (2) concat demuxer ile birleştirilir (kopya); (3) isteğe göre ASS karaoke
/// altyazı (<c>ass</c> filtresi, Windows yolu kaçışlı) ve üstte ilerleme çubuğu (<c>drawbox</c> şerit + görünen genişliği
/// <c>W*t/SÜRE</c> olan dolgu; bkz. <see cref="BuildFinalVideoFilter"/>) basılır,
/// ses AAC olarak eklenir (yoksa sessiz; kısa ses <c>apad</c> ile tamamlanır); hiçbir katman yoksa video yeniden kodlanmaz;
/// (4) küçük resim (jpg) üretilir. İlerleme: normalize %0–55, concat %60, son geçiş %60–95 (<c>-progress</c>), küçük resim %98.
/// </summary>
public sealed class FfmpegComposer : ComposerBase
{
    private readonly IToolLocator _tools;
    private readonly IProcessRunner _runner;
    private readonly IMediaProbe _probe;
    private readonly ICaptionSubtitleWriter _subtitles;
    private readonly ILogger<FfmpegComposer> _logger;

    /// <summary>Birleştirici oluşturur.</summary>
    /// <param name="tools">Araç bulucu (ffmpeg).</param>
    /// <param name="runner">Süreç çalıştırıcı.</param>
    /// <param name="probe">Süre okuyucu (ffprobe).</param>
    /// <param name="subtitles">Altyazı yazıcısı (ASS).</param>
    /// <param name="logger">Günlükçü.</param>
    public FfmpegComposer(IToolLocator tools, IProcessRunner runner, IMediaProbe probe, ICaptionSubtitleWriter subtitles, ILogger<FfmpegComposer> logger)
    {
        _tools = tools;
        _runner = runner;
        _probe = probe;
        _subtitles = subtitles;
        _logger = logger;
    }

    /// <summary>İlerleme çubuğu rengi (ffmpeg renk sözdizimi).</summary>
    public const string ProgressBarColor = "0xF28C28@0.95";

    /// <summary>Boyuta göre ilerleme çubuğu kalınlığı (px, çift sayı, en az 8).</summary>
    /// <param name="height">Video yüksekliği.</param>
    /// <returns>Kalınlık.</returns>
    public static int ProgressBarHeight(int height)
    {
        var h = Math.Max(8, (int)Math.Round(height * 0.008));
        return h % 2 == 0 ? h : h + 1;
    }

    /// <summary>Son geçiş filtre grafiğinin çıkış etiketi.</summary>
    public const string FinalVideoLabel = "[vout]";

    /// <summary>
    /// Son geçişin <c>-filter_complex</c> grafiğini üretir (giriş <c>[0:v]</c>, çıkış <see cref="FinalVideoLabel"/>);
    /// altyazı ve ilerleme çubuğu kapalıysa <c>null</c> (video yeniden kodlanmadan kopyalanır).
    /// Çubuk: <c>drawbox</c> ile yarı saydam üst şerit + zamana göre büyüyen turuncu dolgu. <c>drawbox</c> ifadelerinde <c>t</c>
    /// zaman değil kalınlık olduğu ve ifadeler kare başına değerlendirilmediği için dolgu, <c>overlay</c> filtresinin kare başına
    /// <c>x=-w+w*t/SÜRE</c> ifadesiyle kaydırılan renk şeridiyle çizilir (görünen genişlik = <c>W*t/SÜRE</c>).
    /// </summary>
    /// <param name="subtitlePath">ASS dosyası (altyazı kapalıysa <c>null</c>).</param>
    /// <param name="showProgressBar">İlerleme çubuğu.</param>
    /// <param name="totalDurationSec">Toplam süre (çubuk genişliği için).</param>
    /// <param name="width">Video genişliği.</param>
    /// <param name="height">Video yüksekliği.</param>
    /// <param name="fps">Kare hızı.</param>
    /// <returns>Filtre grafiği ya da <c>null</c>.</returns>
    public static string? BuildFinalVideoFilter(string? subtitlePath, bool showProgressBar, double totalDurationSec, int width, int height, int fps)
    {
        if (subtitlePath is null && !showProgressBar)
        {
            return null;
        }

        var chain = new List<string>();
        if (subtitlePath is not null)
        {
            chain.Add("ass=filename=" + FfmpegFilterEscaper.EscapePath(subtitlePath));
        }

        if (!showProgressBar)
        {
            return "[0:v]" + string.Join(',', chain) + FinalVideoLabel;
        }

        var bar = ProgressBarHeight(height);
        var duration = Math.Max(0.001, totalDurationSec).ToString("0.###", CultureInfo.InvariantCulture);
        chain.Add(string.Create(CultureInfo.InvariantCulture, $"drawbox=x=0:y=0:w=iw:h={bar}:color=black@0.35:t=fill"));
        return string.Create(CultureInfo.InvariantCulture,
            $"[0:v]{string.Join(',', chain)}[base];color=c={ProgressBarColor}:s={width}x{bar}:r={fps},format=rgba[bar];[base][bar]overlay=x=-w+w*t/{duration}:y=0:shortest=1:eval=frame{FinalVideoLabel}");
    }

    /// <summary>Bir sahnenin normalize filtre zinciri (boyut, sınırlı aralık + BT.709, en-boy koruyarak siyah pad, fps, piksel biçimi, gerekirse son kare uzatma).</summary>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <param name="fps">Kare hızı.</param>
    /// <param name="padSec">Son kareyle uzatma süresi (0 = yok).</param>
    /// <returns>Filtre zinciri.</returns>
    public static string BuildNormalizeFilter(int width, int height, int fps, double padSec)
    {
        var sb = new StringBuilder();
        // Remotion/Revideo JPEG karelerinden gelen tam aralık (pc, yuvj420p) bazı oynatıcılarda/platformlarda soluk ya da patlak görünür:
        // her sahne sınırlı aralığa (tv) ve BT.709 matrisine çevrilir, böylece son dosya her motorda standart yuv420p olur.
        sb.Append(CultureInfo.InvariantCulture, $"scale={width}:{height}:force_original_aspect_ratio=decrease:out_range=tv:out_color_matrix=bt709,");
        sb.Append(CultureInfo.InvariantCulture, $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:color=black,setsar=1,fps={fps},format=yuv420p");
        if (padSec > 0)
        {
            sb.Append(CultureInfo.InvariantCulture, $",tpad=stop_mode=clone:stop_duration={padSec.ToString("0.###", CultureInfo.InvariantCulture)}");
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    protected override async Task<Result<ComposeOutput>> ComposeCoreAsync(ComposeRequest request, IProgress<double>? progress, CancellationToken ct)
    {
        var ffmpeg = _tools.Require(ToolKind.Ffmpeg);
        if (ffmpeg.IsFailure)
        {
            return Result.Failure<ComposeOutput>(ffmpeg.Error);
        }

        var work = Path.Combine(request.WorkDir, "compose");
        Directory.CreateDirectory(work);
        progress?.Report(0);

        var normalized = new List<string>();
        var total = 0d;
        for (var i = 0; i < request.ScenePaths.Count; i++)
        {
            var scene = await NormalizeSceneAsync(ffmpeg.Value, request, i, work, ct).ConfigureAwait(false);
            if (scene.IsFailure)
            {
                return Result.Failure<ComposeOutput>(scene.Error);
            }

            normalized.Add(scene.Value.Path);
            total += scene.Value.DurationSec;
            Scale(progress, 0, 55, (i + 1d) / request.ScenePaths.Count);
        }

        var concat = await ConcatAsync(ffmpeg.Value, normalized, work, ct).ConfigureAwait(false);
        if (concat.IsFailure)
        {
            return Result.Failure<ComposeOutput>(concat.Error);
        }

        progress?.Report(60);
        string? subtitlePath = null;
        if (request.BurnSubtitles && request.Captions.Count > 0)
        {
            subtitlePath = Path.Combine(work, "captions" + _subtitles.FileExtension);
            var text = _subtitles.Build(request.Captions, SubtitleLayout.For(request.Width, request.Height));
            await File.WriteAllTextAsync(subtitlePath, text, new UTF8Encoding(true), ct).ConfigureAwait(false);
        }

        var final = await FinalPassAsync(ffmpeg.Value, request, concat.Value, subtitlePath, total, progress, ct).ConfigureAwait(false);
        if (final.IsFailure)
        {
            return Result.Failure<ComposeOutput>(final.Error);
        }

        var duration = await _probe.GetDurationAsync(request.OutputPath, ct).ConfigureAwait(false);
        var actual = duration.IsSuccess ? duration.Value : total;
        var thumbnail = await CreateThumbnailAsync(ffmpeg.Value, request.OutputPath, actual, ct).ConfigureAwait(false);
        progress?.Report(100);
        _logger.LogInformation("Video birleştirildi: {Output} ({Duration:0.00} sn, altyazı={Subs}, çubuk={Bar})", request.OutputPath, actual, subtitlePath is not null, request.ShowProgressBar);
        return new ComposeOutput(request.OutputPath, actual, thumbnail);
    }

    private async Task<Result<NormalizedScene>> NormalizeSceneAsync(string ffmpeg, ComposeRequest request, int index, string work, CancellationToken ct)
    {
        var source = request.ScenePaths[index];
        var measured = await _probe.GetDurationAsync(source, ct).ConfigureAwait(false);
        if (measured.IsFailure)
        {
            return Result.Failure<NormalizedScene>(measured.Error);
        }

        var target = request.SceneDurationsSec[index] > 0 ? request.SceneDurationsSec[index] : measured.Value;
        var pad = Math.Max(0, target - measured.Value);
        var output = Path.Combine(work, string.Create(CultureInfo.InvariantCulture, $"scene-{index:D3}.mp4"));
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "error",
            "-i", source,
            "-an",
            "-vf", BuildNormalizeFilter(request.Width, request.Height, request.Fps, pad > 0 ? pad + 0.05 : 0),
            "-t", Seconds(target),
            "-r", request.Fps.ToString(CultureInfo.InvariantCulture),
            "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p",
            "-color_range", "tv", "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709",
            "-video_track_timescale", "90000",
            output,
        };

        var run = await RunAsync(ffmpeg, args, null, ct).ConfigureAwait(false);
        return run.IsFailure
            ? Result.Failure<NormalizedScene>(Error.Failure("compose.normalize_failed", $"Sahne {index + 1} normalize edilemedi: {run.Error.Message}"))
            : new NormalizedScene(output, target);
    }

    private async Task<Result<string>> ConcatAsync(string ffmpeg, IReadOnlyList<string> scenes, string work, CancellationToken ct)
    {
        var list = Path.Combine(work, "concat.txt");
        await File.WriteAllLinesAsync(list, scenes.Select(FfmpegFilterEscaper.ConcatListLine), new UTF8Encoding(false), ct).ConfigureAwait(false);
        var output = Path.Combine(work, "concat.mp4");
        var run = await RunAsync(ffmpeg, ["-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", list, "-c", "copy", output], null, ct).ConfigureAwait(false);
        return run.IsFailure
            ? Result.Failure<string>(Error.Failure("compose.concat_failed", $"Sahneler birleştirilemedi: {run.Error.Message}"))
            : output;
    }

    private async Task<Result> FinalPassAsync(string ffmpeg, ComposeRequest request, string concat, string? subtitlePath, double total, IProgress<double>? progress, CancellationToken ct)
    {
        var filter = BuildFinalVideoFilter(subtitlePath, request.ShowProgressBar, total, request.Width, request.Height, request.Fps);
        var hasAudio = !string.IsNullOrWhiteSpace(request.AudioPath);
        var args = new List<string> { "-y", "-hide_banner", "-loglevel", "error", "-nostats", "-progress", "pipe:1", "-i", concat };
        if (hasAudio)
        {
            args.AddRange(["-i", request.AudioPath!]);
        }

        if (filter is null)
        {
            args.AddRange(["-map", "0:v:0"]);
        }
        else
        {
            args.AddRange(["-filter_complex", filter, "-map", FinalVideoLabel]);
        }
        if (hasAudio)
        {
            args.AddRange(["-map", "1:a:0", "-af", "apad", "-c:a", "aac", "-b:a", "192k", "-ar", "48000"]);
        }
        else
        {
            args.Add("-an");
        }

        if (filter is null)
        {
            args.AddRange(["-c:v", "copy"]);
        }
        else
        {
            args.AddRange(["-c:v", "libx264", "-preset", "medium", "-crf", "19", "-pix_fmt", "yuv420p", "-r", request.Fps.ToString(CultureInfo.InvariantCulture)]);
            args.AddRange(["-color_range", "tv", "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709"]);
        }

        args.AddRange(["-t", Seconds(total), "-movflags", "+faststart", request.OutputPath]);
        var totalUs = Math.Max(1, total * 1_000_000);
        var run = await RunAsync(ffmpeg, args, line =>
        {
            if (line.StartsWith("out_time_us=", StringComparison.Ordinal)
                && long.TryParse(line.AsSpan("out_time_us=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var us))
            {
                Scale(progress, 60, 95, us / totalUs);
            }
        }, ct).ConfigureAwait(false);

        return run.IsFailure
            ? Error.Failure("compose.final_failed", $"Son video üretilemedi: {run.Error.Message}")
            : Result.Success();
    }

    private async Task<string?> CreateThumbnailAsync(string ffmpeg, string video, double duration, CancellationToken ct)
    {
        var thumb = Path.ChangeExtension(video, ".jpg");
        var at = Math.Min(1.5, duration / 3);
        var run = await RunAsync(ffmpeg, ["-y", "-hide_banner", "-loglevel", "error", "-ss", Seconds(at), "-i", video, "-frames:v", "1", "-vf", "scale='min(720,iw)':-2", "-q:v", "3", thumb], null, ct).ConfigureAwait(false);
        if (run.IsFailure || !File.Exists(thumb))
        {
            _logger.LogWarning("Küçük resim üretilemedi: {Video}", video);
            return null;
        }

        return thumb;
    }

    private async Task<Result> RunAsync(string ffmpeg, IReadOnlyList<string> args, Action<string>? onOutput, CancellationToken ct)
    {
        var result = await _runner.RunAsync(new ProcessSpec(ffmpeg, args, Timeout: TimeSpan.FromHours(1)), onOutput, ct).ConfigureAwait(false);
        if (result.Succeeded)
        {
            return Result.Success();
        }

        var err = result.StandardError.Trim();
        err = err.Length <= 1500 ? err : err[^1500..];
        return Error.Failure("compose.ffmpeg_failed", result.TimedOut ? "ffmpeg zaman aşımına uğradı." : $"ffmpeg kod {result.ExitCode}: {err}");
    }

    private static string Seconds(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
