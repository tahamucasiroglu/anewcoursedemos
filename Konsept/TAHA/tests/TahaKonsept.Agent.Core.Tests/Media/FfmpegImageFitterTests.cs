using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Core.Media.Base;
using TahaKonsept.Agent.Core.Media.Concrete;
using TahaKonsept.Agent.Core.Processes.Concrete;
using TahaKonsept.Agent.Core.Tests.Support;
using TahaKonsept.BuildingBlocks.Domain.Results;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Media;

/// <summary>
/// <see cref="FfmpegImageFitter"/> testleri. Gerçek ffmpeg (<c>.tools-dev/ffmpeg</c>; yoksa atlanır) ile: sd.cpp'nin FLUX.2'de verdiği
/// 1088x1920 görsel istenen 1080x1920'ye kırpılır, esnetilmez; doğru boyuttaki görsele dokunulmaz. Ayrıca ffprobe çıktısı çözümü ve filtre metni.
/// </summary>
public sealed class FfmpegImageFitterTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    /// <summary>1088x1920 → 1080x1920 (oran korunur, ortadan kırpılır); ikinci çağrı değişiklik yapmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Fit_RoundedSdCppImage_IsCroppedToRequestedSize()
    {
        var ct = TestContext.Current.CancellationToken;
        var tools = RealTools();
        Assert.SkipWhen(tools is null, ".tools-dev/ffmpeg bulunamadı.");
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
        var image = Path.Combine(_temp.Path, "scene-0-1.partial.png");
        var made = await runner.RunAsync(
            new ProcessSpec(tools!.Find(ToolKind.Ffmpeg)!, ["-y", "-loglevel", "error", "-f", "lavfi", "-i", "testsrc2=size=1088x1920", "-frames:v", "1", image]),
            null,
            ct);
        made.Succeeded.ShouldBeTrue(made.StandardError);
        var fitter = new FfmpegImageFitter(tools, runner);

        var changed = await fitter.FitAsync(image, 1080, 1920, ct);
        var size = await fitter.GetSizeAsync(image, ct);
        var again = await fitter.FitAsync(image, 1080, 1920, ct);

        changed.IsSuccess.ShouldBeTrue(changed.IsFailure ? changed.Error.Message : null);
        changed.Value.ShouldBeTrue();
        size.Value.ShouldBe((1080, 1920));
        again.Value.ShouldBeFalse();
        Directory.GetFiles(_temp.Path, "*.fit.*").ShouldBeEmpty();
    }

    /// <summary>Okunamayan dosya anlaşılır hata döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Fit_UnreadableFile_ReturnsProbeError()
    {
        var tools = RealTools();
        Assert.SkipWhen(tools is null, ".tools-dev/ffmpeg bulunamadı.");
        var bad = Path.Combine(_temp.Path, "bozuk.png");
        await File.WriteAllBytesAsync(bad, [1, 2, 3, 4], TestContext.Current.CancellationToken);
        var fitter = new FfmpegImageFitter(tools!, new ProcessRunner(NullLogger<ProcessRunner>.Instance));

        var result = await fitter.FitAsync(bad, 1080, 1920, TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("agent.image_probe_failed");
    }

    /// <summary>ffprobe <c>csv=p=0:s=x</c> çıktısı çözülür; bozuk çıktı reddedilir.</summary>
    /// <param name="text">Çıktı.</param>
    /// <param name="ok">Beklenen sonuç.</param>
    /// <param name="width">Beklenen genişlik.</param>
    /// <param name="height">Beklenen yükseklik.</param>
    [Theory]
    [InlineData("1088x1920\r\n", true, 1088, 1920)]
    [InlineData("1080x1920", true, 1080, 1920)]
    [InlineData("", false, 0, 0)]
    [InlineData("N/Ax1920", false, 0, 0)]
    public void TryParseSize_ParsesFfprobeOutput(string text, bool ok, int width, int height)
    {
        ImageFitterBase.TryParseSize(text, out var w, out var h).ShouldBe(ok);
        if (ok)
        {
            (w, h).ShouldBe((width, height));
        }
    }

    /// <summary>Filtre esnetmez: kapla + kırp.</summary>
    [Fact]
    public void BuildFitFilter_CoversThenCrops()
    {
        ImageFitterBase.BuildFitFilter(1080, 1920)
            .ShouldBe("scale=1080:1920:force_original_aspect_ratio=increase:flags=lanczos,crop=1080:1920,setsar=1");
    }

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    private static IToolLocator? RealTools()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TahaKonsept.slnx")))
        {
            dir = dir.Parent;
        }

        var bin = dir is null ? null : Path.Combine(dir.FullName, ".tools-dev", "ffmpeg");
        if (bin is null || !File.Exists(Path.Combine(bin, "ffmpeg.exe")) || !File.Exists(Path.Combine(bin, "ffprobe.exe")))
        {
            return null;
        }

        var tools = Substitute.For<IToolLocator>();
        tools.Find(ToolKind.Ffmpeg).Returns(Path.Combine(bin, "ffmpeg.exe"));
        tools.Find(ToolKind.Ffprobe).Returns(Path.Combine(bin, "ffprobe.exe"));
        tools.Require(ToolKind.Ffmpeg).Returns(Result.Success(Path.Combine(bin, "ffmpeg.exe")));
        tools.Require(ToolKind.Ffprobe).Returns(Result.Success(Path.Combine(bin, "ffprobe.exe")));
        return tools;
    }
}
