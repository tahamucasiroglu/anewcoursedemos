using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Video;
using TahaKonsept.Agent.Abstractions.Video.Abstract;
using TahaKonsept.Agent.Engines.Video.Engines.Concrete;
using TahaKonsept.Agent.Engines.Video.Media.Concrete;
using TahaKonsept.Agent.Engines.Video.Safety.Concrete;
using TahaKonsept.Agent.Engines.Video.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Integration;

/// <summary>
/// Gerçek render: HyperFrames, Remotion ve Revideo ile Türkçe metinli kısa sahneler mp4'e render edilir.
/// Şablonlar <c>.tools-dev/agent-tools/engines</c> altında kurulur (ilk çalıştırma npm + Chrome indirir).
/// node/ffmpeg/.tools-dev yoksa ya da <c>TAHAKONSEPT_SKIP_ENGINE_TESTS=1</c> ise atlanır.
/// </summary>
public sealed class VideoEngineRenderTests
{
    private const string HyperFramesScene = """
        <!doctype html>
        <html lang="tr">
        <head>
        <meta charset="UTF-8" />
        <link rel="stylesheet" href="fonts/tk-fonts.css" />
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }
          html, body { width: 540px; height: 960px; overflow: hidden; background: #1b120c; }
          #root { position: relative; width: 100%; height: 100%; font-family: 'Montserrat', sans-serif; }
          .clip { position: absolute; inset: 0; }
          #bg { background: radial-gradient(circle at 50% 40%, #e2711d 0%, #3d2412 70%); }
          #title { top: 360px; bottom: auto; text-align: center; color: #fff; font-weight: 900; font-size: 56px; padding: 0 40px; opacity: 0; }
        </style>
        </head>
        <body>
        <div id="root" data-composition-id="main" data-start="0" data-duration="2" data-width="540" data-height="960">
          <div id="bg" class="clip" data-start="0" data-duration="2"></div>
          <h1 id="title" class="clip" data-start="0" data-duration="2">İstanbul'un Fethi: ışık, ğ, ş</h1>
        </div>
        <script src="gsap.min.js"></script>
        <script>
          window.__timelines = window.__timelines || {};
          const tl = gsap.timeline({ paused: true });
          tl.fromTo("#title", { opacity: 0, y: 60 }, { opacity: 1, y: 0, duration: 0.6, ease: "power3.out" }, 0.1);
          window.__timelines["main"] = tl;
        </script>
        </body>
        </html>
        """;

    private const string RemotionScene = """
        import React from "react";
        import { AbsoluteFill, interpolate, useCurrentFrame } from "remotion";

        export default function Scene() {
          const frame = useCurrentFrame();
          const opacity = interpolate(frame, [0, 20], [0, 1], { extrapolateRight: "clamp" });
          return (
            <AbsoluteFill style={{ background: "#3d2412", justifyContent: "center", alignItems: "center" }}>
              <div style={{ opacity, color: "white", fontFamily: "Montserrat", fontWeight: 900, fontSize: 56, textAlign: "center" }}>Şimdi Öğren: ığüşöç</div>
            </AbsoluteFill>
          );
        }
        """;

    private const string RevideoScene = """
        import { makeScene2D, Txt, Rect } from '@revideo/2d';
        import { createRef, waitFor, all } from '@revideo/core';

        export default makeScene2D('scene', function* (view) {
          const title = createRef<Txt>();
          view.add(<Rect width={'100%'} height={'100%'} fill={'#3d2412'} />);
          view.add(<Txt ref={title} text={'Işık ve ğ'} fontFamily={'Montserrat'} fontWeight={900} fontSize={60} fill={'#ffffff'} opacity={0} />);
          yield* all(title().opacity(1, 0.5));
          yield* waitFor(1.5);
        });
        """;

    /// <summary>HyperFrames: lint geçer, mp4 üretilir, süre ≈ 2 sn, boyut istenen.</summary>
    [Fact]
    public async Task HyperFrames_RendersTurkishScene()
    {
        Assert.SkipUnless(TestEnvironment.CanRunEngines, "node/ffmpeg/.tools-dev yok ya da motor testleri kapalı.");
        var engine = new HyperFramesVideoEngine(Paths(), Tools, Runner, Probe, new DefaultSceneCodeGuard(), NullLogger<HyperFramesVideoEngine>.Instance);
        await RenderAndAssertAsync(engine, HyperFramesScene, 2.0, validate: true);
    }

    /// <summary>Remotion: mp4 üretilir, süre ≈ 2 sn.</summary>
    [Fact]
    public async Task Remotion_RendersTurkishScene()
    {
        Assert.SkipUnless(TestEnvironment.CanRunEngines, "node/ffmpeg/.tools-dev yok ya da motor testleri kapalı.");
        var engine = new RemotionVideoEngine(Paths(), Tools, Runner, Probe, new DefaultSceneCodeGuard(), NullLogger<RemotionVideoEngine>.Instance);
        await RenderAndAssertAsync(engine, RemotionScene, 2.0, validate: true);
    }

    /// <summary>Revideo (deneysel): mp4 üretilir.</summary>
    [Fact]
    public async Task Revideo_RendersTurkishScene()
    {
        Assert.SkipUnless(TestEnvironment.CanRunEngines, "node/ffmpeg/.tools-dev yok ya da motor testleri kapalı.");
        var engine = new RevideoVideoEngine(Paths(), Tools, Runner, Probe, new DefaultSceneCodeGuard(), NullLogger<RevideoVideoEngine>.Instance);
        await RenderAndAssertAsync(engine, RevideoScene, 2.0, validate: true);
    }

    /// <summary>HyperFrames lint, kök kompozisyonu olmayan kodu LLM'e açıklanabilir hatayla reddeder.</summary>
    [Fact]
    public async Task HyperFrames_ValidateRejectsBrokenComposition()
    {
        Assert.SkipUnless(TestEnvironment.CanRunEngines, "node/ffmpeg/.tools-dev yok ya da motor testleri kapalı.");
        var engine = new HyperFramesVideoEngine(Paths(), Tools, Runner, Probe, new DefaultSceneCodeGuard(), NullLogger<HyperFramesVideoEngine>.Instance);
        var dir = TestEnvironment.NewTempDir("hf-validate");
        var request = new SceneRenderRequest(dir, 0, "<html><body><div id=\"x\" data-composition-id=\"main\"><h1 data-start=\"0\">a</h1></div><script>window.__timelines = {};</script></body></html>", 540, 960, 30, 2, [], Path.Combine(dir, "x.mp4"));

        (await engine.EnsureReadyAsync(null, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var result = await engine.ValidateSceneAsync(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("lint");
    }

    private static readonly TestToolLocator Tools = new();
    private static readonly TestProcessRunner Runner = new();
    private static readonly FfprobeMediaProbe Probe = new(Tools, Runner);

    private static TestAgentPaths Paths() => new(TestEnvironment.NewTempDir("agent"), TestEnvironment.EngineToolsDir);

    private static async Task RenderAndAssertAsync(IVideoEngine engine, string code, double seconds, bool validate)
    {
        var ct = TestContext.Current.CancellationToken;
        var ready = await engine.EnsureReadyAsync(null, ct);
        ready.IsSuccess.ShouldBeTrue(ready.IsFailure ? ready.Error.Message : string.Empty);
        engine.IsInstalled().ShouldBeTrue();

        var dir = TestEnvironment.NewTempDir(engine.Key);
        var request = new SceneRenderRequest(dir, 0, code, 540, 960, 30, seconds, [], Path.Combine(dir, "scene-000.mp4"));
        if (validate)
        {
            var valid = await engine.ValidateSceneAsync(request, ct);
            valid.IsSuccess.ShouldBeTrue(valid.IsFailure ? valid.Error.Message : string.Empty);
        }

        var progress = new List<double>();
        var result = await engine.RenderSceneAsync(request, new SyncProgress(progress.Add), ct);

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        File.Exists(result.Value.VideoPath).ShouldBeTrue();
        result.Value.DurationSec.ShouldBe(seconds, 0.25);
        progress.ShouldContain(100);
    }
}
