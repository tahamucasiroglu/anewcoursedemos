using Shouldly;
using TahaKonsept.Agent.Abstractions.Compose;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Engines.Video.Compose.Concrete;
using TahaKonsept.Agent.Engines.Video.Compose.Encoding;
using TahaKonsept.Agent.Engines.Video.Compose.Encoding.Concrete;
using TahaKonsept.Agent.Engines.Video.Engines.Base;
using TahaKonsept.Agent.Engines.Video.Engines.Concrete;
using TahaKonsept.Agent.Engines.Video.Subtitles.Concrete;
using TahaKonsept.Agent.Engines.Video.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Unit;

/// <summary>
/// "Önce GPU" video kararları (GPU'suz, sahte süreçlerle): NVENC yoklaması ve libx264 yedeği, kodlama sırasında NVENC düşünce
/// birleştiricinin yazılımla yeniden kodlaması, tarayıcı motorlarının GPU/CPU argümanları ve GPU hatası/düşüşü tanıma.
/// </summary>
public sealed class GpuFirstVideoTests
{
    private const string EncodersWithNvenc = " V....D libx264  libx264 H.264\n V....D h264_nvenc  NVIDIA NVENC H.264 encoder (codec h264)\n";

    /// <summary>NVENC argümanları crf karşılığı sabit kalite (vbr + cq + b:v 0) ve yuv420p; yazılım argümanları eski crf/preset.</summary>
    [Fact]
    public void Encoder_arguments_match_quality_targets()
    {
        VideoEncoderChoice.Nvenc.Arguments(VideoEncodePurpose.Final).ShouldBe(
            ["-c:v", "h264_nvenc", "-preset", "p5", "-tune", "hq", "-rc", "vbr", "-cq", "19", "-b:v", "0", "-spatial-aq", "1", "-pix_fmt", "yuv420p"]);
        VideoEncoderChoice.Nvenc.Arguments(VideoEncodePurpose.Intermediate).ShouldContain("18");
        VideoEncoderChoice.Software.Arguments(VideoEncodePurpose.Final).ShouldBe(["-c:v", "libx264", "-preset", "medium", "-crf", "19", "-pix_fmt", "yuv420p"]);
        VideoEncoderChoice.Software.Arguments(VideoEncodePurpose.Intermediate).ShouldBe(["-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p"]);
    }

    /// <summary>ffmpeg h264_nvenc içeriyor ve deneme kodlaması geçiyorsa NVENC; yoklama ffmpeg başına bir kez yapılır.</summary>
    [Fact]
    public async Task Selects_nvenc_when_probe_passes_and_caches_it()
    {
        var runner = new ScriptedProcessRunner(spec => spec.Arguments.Contains("-encoders") ? ScriptedProcessRunner.Ok(EncodersWithNvenc) : ScriptedProcessRunner.Ok());
        var selector = new FfmpegNvencEncoderSelector(runner, new ListLogger<FfmpegNvencEncoderSelector>());
        var ct = TestContext.Current.CancellationToken;

        (await selector.SelectAsync("ffmpeg.exe", ct)).ShouldBe(VideoEncoderChoice.Nvenc);
        (await selector.SelectAsync("ffmpeg.exe", ct)).ShouldBe(VideoEncoderChoice.Nvenc);

        runner.Calls.Count.ShouldBe(2);
        runner.Calls[1].Arguments.ShouldBe(FfmpegNvencEncoderSelector.BuildProbeArguments());
    }

    /// <summary>ffmpeg derlemesinde NVENC yoksa libx264'e düşer, nedeni döner ve uyarı loglanır; deneme kodlaması yapılmaz.</summary>
    [Fact]
    public async Task Falls_back_to_libx264_when_build_has_no_nvenc()
    {
        var runner = new ScriptedProcessRunner(_ => ScriptedProcessRunner.Ok(" V....D libx264  libx264 H.264\n"));
        var logger = new ListLogger<FfmpegNvencEncoderSelector>();
        var selector = new FfmpegNvencEncoderSelector(runner, logger);

        var choice = await selector.SelectAsync("ffmpeg.exe", TestContext.Current.CancellationToken);

        choice.Encoder.ShouldBe("libx264");
        choice.IsGpu.ShouldBeFalse();
        choice.FallbackReason.ShouldNotBeNull().ShouldContain("h264_nvenc yok");
        runner.Calls.Count.ShouldBe(1);
        logger.Warnings.ShouldContain(w => w.Contains("libx264", StringComparison.Ordinal));
    }

    /// <summary>NVENC derlemede var ama deneme kodlaması (GPU/sürücü yok) başarısızsa libx264'e düşer ve uyarı loglanır.</summary>
    [Fact]
    public async Task Falls_back_to_libx264_when_trial_encode_fails()
    {
        var runner = new ScriptedProcessRunner(spec => spec.Arguments.Contains("-encoders")
            ? ScriptedProcessRunner.Ok(EncodersWithNvenc)
            : ScriptedProcessRunner.Fail("Cannot load nvcuda.dll"));
        var logger = new ListLogger<FfmpegNvencEncoderSelector>();
        var selector = new FfmpegNvencEncoderSelector(runner, logger);

        var choice = await selector.SelectAsync("ffmpeg.exe", TestContext.Current.CancellationToken);

        choice.IsGpu.ShouldBeFalse();
        choice.FallbackReason.ShouldNotBeNull().ShouldContain("nvcuda.dll");
        logger.Warnings.ShouldNotBeEmpty();
    }

    /// <summary>
    /// NVENC sahne normalize ederken düşerse birleştirici seçiciyi yazılıma çevirir (uyarı), tüm sahneleri libx264 ile baştan normalize eder
    /// ve son geçişi de libx264 ile yapar; sonuç başarılıdır.
    /// </summary>
    [Fact]
    public async Task Composer_reencodes_with_libx264_when_nvenc_fails_mid_compose()
    {
        var runner = new ScriptedProcessRunner(spec =>
            spec.Arguments.Contains("-encoders") ? ScriptedProcessRunner.Ok(EncodersWithNvenc)
            : spec.Arguments.Contains("lavfi") ? ScriptedProcessRunner.Ok()
            : spec.Arguments.Contains("h264_nvenc") ? ScriptedProcessRunner.Fail("OpenEncodeSessionEx failed: out of memory (10)")
            : ScriptedProcessRunner.Ok());
        var selectorLog = new ListLogger<FfmpegNvencEncoderSelector>();
        var selector = new FfmpegNvencEncoderSelector(runner, selectorLog);
        var composer = new FfmpegComposer(new FixedToolLocator(), runner, new FixedDurationProbe(1.0), new AssKaraokeSubtitleWriter(), selector, new ListLogger<FfmpegComposer>());
        var request = Request("nvenc-mid");

        var result = await composer.ComposeAsync(request, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        var encodes = runner.Calls.Where(c => c.Arguments.Contains("-c:v") && !c.Arguments.Contains("lavfi")).Select(c => c.Arguments[c.Arguments.ToList().IndexOf("-c:v") + 1]).ToList();
        encodes.ShouldBe(["h264_nvenc", "libx264", "libx264", "libx264"]); // 1 başarısız NVENC sahnesi, 2 sahne baştan + son geçiş yazılımla
        selectorLog.Warnings.ShouldContain(w => w.Contains("NVENC", StringComparison.Ordinal));
        (await selector.SelectAsync("ffmpeg.exe", TestContext.Current.CancellationToken)).IsGpu.ShouldBeFalse();
    }

    /// <summary>NVENC yalnız son geçişte düşerse son geçiş libx264 ile yeniden yapılır.</summary>
    [Fact]
    public async Task Composer_retries_final_pass_with_libx264()
    {
        var runner = new ScriptedProcessRunner(spec =>
            spec.Arguments.Contains("-encoders") ? ScriptedProcessRunner.Ok(EncodersWithNvenc)
            : spec.Arguments.Contains("-filter_complex") && spec.Arguments.Contains("h264_nvenc") ? ScriptedProcessRunner.Fail("nvenc: no capable devices found")
            : ScriptedProcessRunner.Ok());
        var selector = new FfmpegNvencEncoderSelector(runner, new ListLogger<FfmpegNvencEncoderSelector>());
        var composer = new FfmpegComposer(new FixedToolLocator(), runner, new FixedDurationProbe(1.0), new AssKaraokeSubtitleWriter(), selector, new ListLogger<FfmpegComposer>());

        var result = await composer.ComposeAsync(Request("nvenc-final"), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        var finals = runner.Calls.Where(c => c.Arguments.Contains("-filter_complex")).ToList();
        finals.Count.ShouldBe(2);
        finals[0].Arguments.ShouldContain("h264_nvenc");
        finals[1].Arguments.ShouldContain("libx264");
        runner.Calls.Where(c => c.Arguments.Contains("-vf") && c.Arguments.Contains("-an")).ShouldAllBe(c => c.Arguments.Contains("h264_nvenc"));
    }

    /// <summary>Tarayıcı motorlarının GPU açık/kapalı argümanları: GPU'da donanım GL/NVENC, CPU yedeğinde SwiftShader.</summary>
    [Fact]
    public void Browser_engines_build_gpu_and_cpu_arguments()
    {
        HyperFramesVideoEngine.GpuArguments(true).ShouldBe(["--gpu"]);
        HyperFramesVideoEngine.GpuArguments(false).ShouldBe(["--no-browser-gpu"]);

        RemotionVideoEngine.GpuArguments(true).ShouldBe(["--gl=angle", "--hardware-acceleration=if-possible"]);
        RemotionVideoEngine.GpuArguments(false).ShouldContain("--gl=swangle");

        RevideoVideoEngine.ChromeGpuArguments(true).ShouldContain("--use-angle=d3d11");
        RevideoVideoEngine.ChromeGpuArguments(true).ShouldContain("--enable-gpu-rasterization");
        RevideoVideoEngine.ChromeGpuArguments(true).ShouldContain("--ignore-gpu-blocklist");
        RevideoVideoEngine.ChromeGpuArguments(false).ShouldContain("--use-angle=swiftshader");
        RevideoVideoEngine.ChromeGpuArguments(true).ShouldNotContain(a => a.Contains("proxy", StringComparison.Ordinal));
    }

    /// <summary>Başarısız render günlüğü GPU/tarayıcı kaynaklıysa CPU ile yeniden denenir; sahne kodu hatasında denenmez.</summary>
    [Fact]
    public void Recognizes_gpu_failures_and_tool_fallbacks()
    {
        NodeVideoEngineBase.IsLikelyGpuFailure("Error: GPU process exited unexpectedly").ShouldBeTrue();
        NodeVideoEngineBase.IsLikelyGpuFailure("Failed to launch the browser process!").ShouldBeTrue();
        NodeVideoEngineBase.IsLikelyGpuFailure("SyntaxError: Unexpected token '<' in Scene.tsx").ShouldBeFalse();
        NodeVideoEngineBase.IsLikelyGpuFailure(null).ShouldBeFalse();

        NodeVideoEngineBase.DetectGpuFallback("ok\n[hyperframes] browserGpuMode probe → software (WebGL unavailable)\nbitti")
            .ShouldBe("[hyperframes] browserGpuMode probe → software (WebGL unavailable)");
        NodeVideoEngineBase.DetectGpuFallback("[hyperframes] browserGpuMode probe → hardware (vendor=\"NVIDIA\")").ShouldBeNull();
    }

    private static ComposeRequest Request(string name)
    {
        var dir = TestEnvironment.NewTempDir(name);
        var a = Path.Combine(dir, "a.mp4");
        var b = Path.Combine(dir, "b.mp4");
        File.WriteAllBytes(a, []);
        File.WriteAllBytes(b, []);
        return new ComposeRequest(dir, [a, b], [1.0, 1.0], null, [], BurnSubtitles: false, ShowProgressBar: true, 540, 960, 30, Path.Combine(dir, "out", "final.mp4"));
    }
}
