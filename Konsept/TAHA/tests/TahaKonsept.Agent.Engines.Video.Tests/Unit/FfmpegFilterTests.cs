using Shouldly;
using TahaKonsept.Agent.Engines.Video.Compose;
using TahaKonsept.Agent.Engines.Video.Compose.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Unit;

/// <summary>ffmpeg filtre kaçışı ve son geçiş filtre zincirinin seçeneklere göre açılıp kapanması.</summary>
public sealed class FfmpegFilterTests
{
    /// <summary>Filtre değeri iki düzeyde kaçırılır (ffmpeg belgesindeki örnek).</summary>
    [Fact]
    public void EscapeOptionValue_MatchesFfmpegDocumentationExample() =>
        FfmpegFilterEscaper.EscapeOptionValue("this is a 'string': may contain one, or more, special characters")
            .ShouldBe(@"this is a \\\'string\\\'\\: may contain one\, or more\, special characters");

    /// <summary>Windows yolu: sürücü iki noktası kaçırılır, ters bölü ileri bölüye döner.</summary>
    [Fact]
    public void EscapePath_HandlesWindowsDriveColon()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        FfmpegFilterEscaper.EscapePath(@"C:\Kullanıcı\iş klasörü\altyazı.ass").ShouldBe(@"C\\:/Kullanıcı/iş klasörü/altyazı.ass");
    }

    /// <summary>Concat satırında tek tırnak kaçırılır.</summary>
    [Fact]
    public void ConcatListLine_EscapesSingleQuote() =>
        FfmpegFilterEscaper.ConcatListLine(Path.Combine(Path.GetTempPath(), "it's.mp4")).ShouldEndWith("it'\\''s.mp4'");

    /// <summary>Altyazı ve çubuk kapalıysa filtre yok (video kopyalanır); her seçenek kendi filtresini ekler.</summary>
    [Fact]
    public void BuildFinalVideoFilter_TogglesEachOption()
    {
        FfmpegComposer.BuildFinalVideoFilter(null, false, 10, 1080, 1920, 30).ShouldBeNull();

        var barOnly = FfmpegComposer.BuildFinalVideoFilter(null, true, 12.5, 1080, 1920, 30)!;
        barOnly.ShouldContain("drawbox=x=0:y=0:w=iw:h=16");
        barOnly.ShouldContain("color=c=0xF28C28@0.95:s=1080x16:r=30");
        barOnly.ShouldContain("overlay=x=-w+w*t/12.5:y=0");
        barOnly.ShouldEndWith(FfmpegComposer.FinalVideoLabel);
        barOnly.ShouldNotContain("ass=");

        var subsOnly = FfmpegComposer.BuildFinalVideoFilter(Path.Combine(Path.GetTempPath(), "a.ass"), false, 10, 1920, 1080, 30)!;
        subsOnly.ShouldStartWith("[0:v]ass=filename=");
        subsOnly.ShouldNotContain("drawbox");
    }

    /// <summary>Normalize zinciri: kısa sahnede son kare uzatılır.</summary>
    [Fact]
    public void BuildNormalizeFilter_AddsTpadOnlyWhenNeeded()
    {
        FfmpegComposer.BuildNormalizeFilter(1080, 1920, 30, 0).ShouldNotContain("tpad");
        FfmpegComposer.BuildNormalizeFilter(1080, 1920, 30, 1.25).ShouldEndWith("tpad=stop_mode=clone:stop_duration=1.25");
    }
}
