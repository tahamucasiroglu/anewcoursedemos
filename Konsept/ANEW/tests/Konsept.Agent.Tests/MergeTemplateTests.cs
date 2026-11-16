using System.Globalization;
using System.Text.RegularExpressions;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using NSubstitute;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Birleştirmenin sabit şablonu ve araç denetimi (spec 0017 Req 2–4, 14, 14a, 19): altyazı kelime kelime zamanlı ve vurgulu, ilerleme çubuğu
/// zamanla orantılı, sessiz iz, NVENC, AI ses notu. Gerçek FFmpeg çıktısının kare düzeyinde kanıtı kullanıcı testindedir (B-119).
/// </summary>
public sealed partial class MergeTemplateTests
{
    /// <summary>
    /// AC-1: her kelime kendi sahnesinin ses aralığında ve tam bir kez vurgulu (farklı renk); sahne aralıkları ardışık; altyazı LLM'den gelmez
    /// (yalnızca anlatım metninden).
    /// </summary>
    [Fact]
    public void Subtitles_KaraokePerWord_OnlyWhenEnabled()
    {
        var ass = MergeTemplate.Subtitles([(4d, "Bizans son sabah"), (6d, "Bir çağ kapanıyor")], 1920, 1080);
        var dialogues = ass.Split('\n').Where(l => l.StartsWith("Dialogue:", StringComparison.Ordinal)).ToList();

        dialogues.Count.ShouldBe(6);
        var spans = dialogues.Select(d => d.Split(',')).Select(p => (Start: Seconds(p[1]), End: Seconds(p[2]))).ToList();
        spans.Take(3).ShouldAllBe(s => s.Start >= 0 && s.End <= 4.001);
        spans.Skip(3).ShouldAllBe(s => s.Start >= 3.999 && s.End <= 10.001);
        spans[0].Start.ShouldBe(0);
        spans[^1].End.ShouldBe(10, 0.011);
        for (var i = 1; i < spans.Count; i++)
        {
            spans[i].Start.ShouldBe(spans[i - 1].End, 0.011);
        }

        dialogues.ShouldAllBe(d => Highlight().Matches(d).Count == 1);
        dialogues[0].ShouldContain("{\\c" + MergeTemplate.HighlightColor + "}Bizans{\\c" + MergeTemplate.TextColor + "} son sabah");
        dialogues[4].ShouldContain("Bir {\\c" + MergeTemplate.HighlightColor + "}çağ");
        MergeTemplate.HighlightColor.ShouldNotBe(MergeTemplate.TextColor);

        MergeTemplate.Filter(subtitles: true, progressBar: false, 10, 1920).ShouldContain("subtitles=" + MergeTemplate.SubtitleFile);
        MergeTemplate.Filter(subtitles: false, progressBar: false, 10, 1920).ShouldNotContain("subtitles");
    }

    /// <summary>AC-2: çubuk üst kenarda (y=0), x = -w + w·t/T: ilk karede boş, sonda dolu, yarıda yarı (±%5); kapalıyken çubuk yok.</summary>
    [Fact]
    public void ProgressBar_TopEdge_ProportionalToTime()
    {
        var filter = MergeTemplate.Filter(subtitles: false, progressBar: true, 60, 1920);
        filter.ShouldContain("color=c=" + MergeTemplate.BarColor + ":s=1920x" + MergeTemplate.BarHeight.ToString(CultureInfo.InvariantCulture));
        filter.ShouldContain("overlay=x='-w+w*t/60':y=0");
        filter.ShouldEndWith("format=yuv420p[v]");

        static double Visible(double t, double total, double w) => w + (-w + (w * t / total)); // Görünen genişlik = x + w.
        Visible(0, 60, 1920).ShouldBe(0);
        Visible(60, 60, 1920).ShouldBe(1920);
        (Visible(30, 60, 1920) / 1920).ShouldBe(0.5, 0.05);

        MergeTemplate.Filter(subtitles: false, progressBar: false, 60, 1920).ShouldNotContain("overlay");
    }

    /// <summary>AC-3: ses yoksa sessiz iz (anullsrc) videonun süresi kadar; ses varsa ses listesi; kodlama NVENC.</summary>
    [Fact]
    public void Silent_AnullsrcNoSubtitles()
    {
        var silent = MergeTemplate.Arguments("clips.txt", null, "[0:v]format=yuv420p[v]", 42.5, "out.mp4");
        silent.ShouldContain("anullsrc=r=48000:cl=stereo");
        silent[silent.ToList().IndexOf("-t") + 1].ShouldBe("42.5");
        var voiced = MergeTemplate.Arguments("clips.txt", "voice.txt", "[0:v]format=yuv420p[v]", 42.5, "out.mp4");
        voiced.ShouldNotContain("anullsrc=r=48000:cl=stereo");
        voiced.ShouldContain("voice.txt");
        MergeTemplate.ConcatList([@"C:\a b\it's.mp4"]).ShouldBe("file 'C:/a b/it'\\''s.mp4'\n");
    }

    /// <summary>AC-31 (kodlama kısmı): kodlayıcı NVENC; NVENC/CUDA hatası GPU hatası sayılır (CPU kodlayıcısına düşülmez).</summary>
    [Fact]
    public void Encoder_Nvenc_NoCpuFallback()
    {
        var args = MergeTemplate.Arguments("c.txt", null, "f", 1, "o.mp4");
        args[args.ToList().IndexOf("-c:v") + 1].ShouldBe("h264_nvenc");
        args.ShouldNotContain("libx264");
        MergeTemplate.IsGpuFailure("[h264_nvenc @ 0x1] Cannot load nvcuda.dll").ShouldBeTrue();
        MergeTemplate.IsGpuFailure("No capable devices found").ShouldBeTrue();
        MergeTemplate.IsGpuFailure("Invalid data found when processing input").ShouldBeFalse();
    }

    /// <summary>AC-22: AI ses notu yalnızca Supertonic 3 ile; videonun yanında ayrı dosya (kareye gömülmez); başka ses ya da sessizde yok.</summary>
    [Fact]
    public void AiVoiceNotice_OnlySupertonic()
    {
        AiVoiceNotice.Applies("local/supertonic-3").ShouldBeTrue();
        AiVoiceNotice.Applies("local/chatterbox-multilingual-v3").ShouldBeFalse();
        AiVoiceNotice.Applies(null).ShouldBeFalse();
        AiVoiceNotice.Text.ShouldContain("Yapay zekâ ile üretilmiş ses");
        AiVoiceNotice.FileFor(@"C:\Videolar\anewconcept\Kuşatma-1a2b3c4d.mp4").ShouldBe(@"C:\Videolar\anewconcept\Kuşatma-1a2b3c4d.ai-ses.txt");
        MergeTemplate.Filter(true, true, 10, 1920).ShouldNotContain("Yapay");
    }

    /// <summary>AC-16: ajan kurulu olmayan araçları bildirir; yerel ses modeli koşucusu olmadığından seslendirme "kurulu değil" döner (B-116).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task StageRunner_ReportsMissingTools()
    {
        var store = Substitute.For<Konsept.Agent.Application.Abstract.ISetupStateStore>();
        var installed = new Dictionary<string, InstalledComponent>
        {
            ["node"] = new("26.13.1", "x"),
            ["ffmpeg"] = new("8.1.3", "y"),
        };
        store.Load().Returns(new SetupState(installed, null));
        var runner = new ProcessStageRunner(store, Path.GetTempPath(), Path.GetTempPath, Substitute.For<Konsept.Agent.Application.Abstract.ISceneRenderer>(), Serilog.Core.Logger.None);

        runner.CheckTools(new AgentToolCheckRequest(["node", "ffmpeg", "chromium", "hyperframes", "supertonic-3"])).Missing
            .ShouldBe(["chromium", "hyperframes", "supertonic-3"]);
        var voice = await runner.RunAsync(new AgentStageCommand
        {
            Stage = AgentStageCommand.Voice,
            Engine = "local/hyperframes",
            AudioModel = "local/supertonic-3",
            Language = "tr",
            Format = "landscape",
        });
        (voice.Ok, voice.Error, voice.Detail).ShouldBe((false, AgentStageReply.ToolMissing, "supertonic-3"));
        var render = await runner.RunAsync(new AgentStageCommand { Stage = AgentStageCommand.Render, Engine = "local/hyperframes", Language = "tr", Format = "landscape" });
        (render.Error, render.Detail).ShouldBe((AgentStageReply.ToolMissing, "hyperframes"));
    }

    private static double Seconds(string time)
    {
        var p = time.Split(':');
        return (int.Parse(p[0], CultureInfo.InvariantCulture) * 3600) + (int.Parse(p[1], CultureInfo.InvariantCulture) * 60) + double.Parse(p[2], CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"\{\\c&H0000B7F5&\}")]
    private static partial Regex Highlight();
}
