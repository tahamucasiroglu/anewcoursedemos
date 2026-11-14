using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Align;
using TahaKonsept.Agent.Abstractions.Compose;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Engines.Video.Compose.Concrete;
using TahaKonsept.Agent.Engines.Video.Media.Concrete;
using TahaKonsept.Agent.Engines.Video.Subtitles.Concrete;
using TahaKonsept.Agent.Engines.Video.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Integration;

/// <summary>
/// Gerçek ffmpeg ile birleştirme: kısa sahne son kareyle uzatılır, süreler toplanır, altyazı + çubuk + ses + küçük resim.
/// Klasör adında boşluk, tırnak ve virgül var (Windows filtre yolu kaçışı da sınanır). ffmpeg yoksa atlanır.
/// </summary>
public sealed class FfmpegComposerIntegrationTests
{
    private readonly TestToolLocator _tools = new();
    private readonly TestProcessRunner _runner = new();

    /// <summary>Tüm seçenekler açık: süre doğru, ses var, küçük resim var.</summary>
    [Fact]
    public async Task Compose_WithSubtitlesBarAndAudio_ProducesCorrectDuration()
    {
        Assert.SkipUnless(TestEnvironment.HasFfmpeg, "ffmpeg bulunamadı (TAHAKONSEPT_FFMPEG_DIR ya da .tools-dev/ffmpeg).");
        var ct = TestContext.Current.CancellationToken;
        var dir = TestEnvironment.NewTempDir("compose");
        var a = Path.Combine(dir, "a.mp4");
        var b = Path.Combine(dir, "b.mp4");
        var audio = Path.Combine(dir, "ses.wav");
        await MediaFactory.CreateVideoAsync(a, 1.0, 640, 360, 25);   // yatay ve kısa → dikeye pad + 1 sn uzatma
        await MediaFactory.CreateVideoAsync(b, 2.0, 360, 640, 30);   // uzun → 1.5 sn'ye kesilir
        await MediaFactory.CreateAudioAsync(audio, 3.2);

        var captions = new List<CaptionWord>
        {
            new("Merhaba", 100, 600), new("dünya,", 600, 1100), new("İstanbul'un", 1100, 1800), new("fethi", 1800, 2300), new("çağ", 2300, 2700), new("açtı.", 2700, 3300),
        };
        var output = Path.Combine(dir, "out", "final.mp4");
        var progress = new List<double>();
        var request = new ComposeRequest(dir, [a, b], [2.0, 1.5], audio, captions, BurnSubtitles: true, ShowProgressBar: true, 540, 960, 30, output);

        var result = await CreateComposer().ComposeAsync(request, new SyncProgress(progress.Add), ct);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        File.Exists(output).ShouldBeTrue();
        result.Value.DurationSec.ShouldBe(3.5, 0.1);
        result.Value.ThumbnailPath.ShouldNotBeNull();
        File.Exists(result.Value.ThumbnailPath).ShouldBeTrue();
        File.ReadAllText(Path.Combine(dir, "compose", "captions.ass")).ShouldContain("{\\k");
        (await Probe(output, "stream=width,height", "v:0")).ShouldBe("540,960");
        (await Probe(output, "stream=codec_name", "a:0")).ShouldBe("aac");
        progress.ShouldNotBeEmpty();
        progress[^1].ShouldBe(100);

        // İlerleme çubuğu zamanla büyür: genişlik ≈ W * t / toplam süre.
        (await OrangeBarWidth(output, 0.875, 540, dir)).ShouldBe(135, 20);
        (await OrangeBarWidth(output, 2.625, 540, dir)).ShouldBe(405, 20);
    }

    /// <summary>Altyazı/çubuk kapalı ve ses yok: video sessiz, kopyalanır, süre yine doğru.</summary>
    [Fact]
    public async Task Compose_WithEverythingOff_IsSilentAndCorrectLength()
    {
        Assert.SkipUnless(TestEnvironment.HasFfmpeg, "ffmpeg bulunamadı.");
        var ct = TestContext.Current.CancellationToken;
        var dir = TestEnvironment.NewTempDir("compose-off");
        var a = Path.Combine(dir, "a.mp4");
        await MediaFactory.CreateVideoAsync(a, 1.0, 320, 240, 30);
        var output = Path.Combine(dir, "plain.mp4");
        var request = new ComposeRequest(dir, [a, a], [1.0, 2.0], null, [], BurnSubtitles: false, ShowProgressBar: false, 320, 240, 30, output);

        var result = await CreateComposer().ComposeAsync(request, null, ct);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        result.Value.DurationSec.ShouldBe(3.0, 0.1);
        (await Probe(output, "stream=codec_type", "a")).ShouldBeEmpty();
        File.Exists(Path.Combine(dir, "compose", "captions.ass")).ShouldBeFalse();
        (await Probe(output, "stream=codec_name", "v:0")).ShouldBe("h264");
    }

    /// <summary>Yalnızca ilerleme çubuğu açıkken altyazı dosyası yazılmaz ama çubuk vardır.</summary>
    [Fact]
    public async Task Compose_BarOnly_DrawsBarWithoutSubtitles()
    {
        Assert.SkipUnless(TestEnvironment.HasFfmpeg, "ffmpeg bulunamadı.");
        var ct = TestContext.Current.CancellationToken;
        var dir = TestEnvironment.NewTempDir("compose-bar");
        var a = Path.Combine(dir, "a.mp4");
        await MediaFactory.CreateVideoAsync(a, 2.0, 320, 240, 30);
        var output = Path.Combine(dir, "bar.mp4");
        var captions = new List<CaptionWord> { new("yok", 0, 500) };
        var request = new ComposeRequest(dir, [a], [2.0], null, captions, BurnSubtitles: false, ShowProgressBar: true, 320, 240, 30, output);

        var result = await CreateComposer().ComposeAsync(request, null, ct);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        File.Exists(Path.Combine(dir, "compose", "captions.ass")).ShouldBeFalse();
        (await OrangeBarWidth(output, 1.0, 320, dir)).ShouldBe(160, 16);
    }

    /// <summary>
    /// Remotion/Revideo gibi JPEG karelerinden gelen tam aralıklı (yuvj420p, pc) sahne: son dosya standart sınırlı aralıklı
    /// yuv420p (tv, BT.709) olur — hem çubuk/altyazı yeniden kodlamasında hem de her şey kapalıyken (kopyalama) yolunda.
    /// </summary>
    /// <param name="overlays">true = altyazı + çubuk açık (son geçiş yeniden kodlar), false = hepsi kapalı (kopyalama).</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Compose_FullRangeScene_ProducesLimitedRangeYuv420p(bool overlays)
    {
        Assert.SkipUnless(TestEnvironment.HasFfmpeg, "ffmpeg bulunamadı.");
        var ct = TestContext.Current.CancellationToken;
        var dir = TestEnvironment.NewTempDir("compose-range");
        var a = Path.Combine(dir, "jpeg-range.mp4");
        var ffmpeg = _tools.Find(ToolKind.Ffmpeg)!;
        var make = new List<string>
        {
            "-v", "error", "-y", "-f", "lavfi", "-i", "testsrc2=s=320x240:r=30:d=1",
            "-vf", "scale=out_range=pc,format=yuvj420p", "-c:v", "libx264", "-preset", "ultrafast", "-color_range", "pc", a,
        };
        (await _runner.RunAsync(new ProcessSpec(ffmpeg, make), null, ct)).Succeeded.ShouldBeTrue();
        (await Probe(a, "stream=pix_fmt", "v:0")).ShouldBe("yuvj420p");
        var output = Path.Combine(dir, "range.mp4");
        var captions = new List<CaptionWord> { new("Merhaba", 0, 500) };
        var request = new ComposeRequest(dir, [a], [1.0], null, captions, BurnSubtitles: overlays, ShowProgressBar: overlays, 320, 240, 30, output);

        var result = await CreateComposer().ComposeAsync(request, null, ct);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        (await Probe(output, "stream=pix_fmt,color_range,color_space", "v:0")).ShouldBe("yuv420p,tv,bt709");
    }

    /// <summary>Eksik sahne dosyası anlaşılır hata döner.</summary>
    [Fact]
    public async Task Compose_MissingScene_ReturnsNotFound()
    {
        var request = new ComposeRequest(Path.GetTempPath(), ["yok.mp4"], [1.0], null, [], false, false, 320, 240, 30, "x.mp4");
        var result = await CreateComposer().ComposeAsync(request, null, TestContext.Current.CancellationToken);
        result.Error.Code.ShouldBe("compose.scene_missing");
    }

    private FfmpegComposer CreateComposer() =>
        new(_tools, _runner, new FfprobeMediaProbe(_tools, _runner), new AssKaraokeSubtitleWriter(), NullLogger<FfmpegComposer>.Instance);

    private async Task<double> OrangeBarWidth(string video, double atSec, int width, string dir)
    {
        var ffmpeg = _tools.Find(ToolKind.Ffmpeg)!;
        var raw = Path.Combine(dir, $"row-{Guid.NewGuid():N}.rgb");
        var args = new List<string> { "-v", "error", "-y", "-ss", atSec.ToString(System.Globalization.CultureInfo.InvariantCulture), "-i", video, "-frames:v", "1", "-vf", "format=rgb24,crop=iw:1:0:3", "-f", "rawvideo", "-pix_fmt", "rgb24", raw };
        (await _runner.RunAsync(new ProcessSpec(ffmpeg, args), null, CancellationToken.None)).Succeeded.ShouldBeTrue();
        var bytes = await File.ReadAllBytesAsync(raw);
        var count = 0;
        for (var x = 0; x < width && (x * 3) + 2 < bytes.Length; x++)
        {
            int r = bytes[x * 3], g = bytes[(x * 3) + 1], b = bytes[(x * 3) + 2];
            if (r > 200 && g is > 100 and < 180 && b < 90)
            {
                count++;
            }
        }

        return count;
    }

    private async Task<string> Probe(string file, string entries, string stream)
    {
        var ffprobe = _tools.Find(ToolKind.Ffprobe)!;
        var r = await _runner.RunAsync(new ProcessSpec(ffprobe, ["-v", "error", "-select_streams", stream, "-show_entries", entries, "-of", "csv=p=0", file]), null, CancellationToken.None);
        return r.StandardOutput.Trim();
    }
}
