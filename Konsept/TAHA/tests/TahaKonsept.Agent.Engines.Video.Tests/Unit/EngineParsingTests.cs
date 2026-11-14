using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Agent.Engines.Video.Engines.Base;
using TahaKonsept.Agent.Engines.Video.Engines.Concrete;
using TahaKonsept.Agent.Engines.Video.Safety.Concrete;
using TahaKonsept.Agent.Engines.Video.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Unit;

/// <summary>Motorların ilerleme ayrıştırma, kök boyut düzeltme, lint okuma ve TSX import denetimi.</summary>
public sealed class EngineParsingTests
{
    private static readonly TestAgentPaths Paths = new(Path.GetTempPath());

    /// <summary>HyperFrames @hf-progress satırı yüzdeye çevrilir.</summary>
    [Fact]
    public void HyperFrames_ParsesProgressJson()
    {
        var engine = CreateHyperFrames();
        engine.ParseProgress("@hf-progress {\"code\":\"capture\",\"done\":26,\"total\":120,\"pct\":35}").ShouldBe(35);
        engine.ParseProgress("  ███  35%  Capturing frame 26/120").ShouldBeNull();
    }

    /// <summary>Kök öğenin boyutları istenen değere zorlanır.</summary>
    [Fact]
    public void HyperFrames_ForcesRootSize()
    {
        var html = "<div id=\"root\" data-composition-id=\"main\" data-width=\"1920\" data-height=\"1080\" data-duration=\"5\"><div data-width=\"5\"></div></div>";
        var fixedHtml = HyperFramesVideoEngine.ForceRootSize(html, 1080, 1920);
        fixedHtml.ShouldStartWith("<div id=\"root\" data-composition-id=\"main\" data-width=\"1080\" data-height=\"1920\" data-duration=\"5\">");
        fixedHtml.ShouldContain("<div data-width=\"5\">");

        HyperFramesVideoEngine.ForceRootSize("<div data-composition-id=\"x\">", 640, 360).ShouldBe("<div data-composition-id=\"x\" data-width=\"640\" data-height=\"360\">");
    }

    /// <summary>lint JSON çıktısından yalnızca hata düzeyleri okunur.</summary>
    [Fact]
    public void HyperFrames_ParsesLintErrorsOnly()
    {
        const string json = "{\"ok\":false,\"findings\":[{\"code\":\"root_missing_composition_id\",\"severity\":\"error\",\"message\":\"Eksik id\",\"fixHint\":\"Ekle\"},{\"code\":\"w\",\"severity\":\"warning\",\"message\":\"uyarı\"}]}";
        HyperFramesVideoEngine.ParseLintReport(json).ShouldBe("- [root_missing_composition_id] Eksik id Öneri: Ekle");
        HyperFramesVideoEngine.ParseLintReport("{\"ok\":true,\"findings\":[]}").ShouldBe(string.Empty);
        HyperFramesVideoEngine.ParseLintReport("çöp").ShouldBeNull();
    }

    /// <summary>Remotion Rendered/Encoded satırları %0–90 / %90–100.</summary>
    [Fact]
    public void Remotion_ParsesRenderedAndEncoded()
    {
        var engine = new RemotionVideoEngine(Paths, new TestToolLocator(), new TestProcessRunner(), new FixedDurationProbe(1), new DefaultSceneCodeGuard(), NullLogger<RemotionVideoEngine>.Instance);
        engine.ParseProgress("Rendered 45/90, time remaining: 2s").ShouldBe(45);
        engine.ParseProgress("Encoded 90/90").ShouldBe(100);
        engine.ParseProgress("Bundling 50%").ShouldBeNull();
    }

    /// <summary>TSX importları çıkarılır.</summary>
    [Fact]
    public void Tsx_FindsImports() =>
        TsxVideoEngineBase.FindImports("import React from 'react';\nimport { AbsoluteFill, useCurrentFrame } from \"remotion\";\nimport type { X } from 'zod';\nimport './x.css';")
            .ShouldBe(["react", "remotion", "zod", "./x.css"]);

    /// <summary>Yapı denetimi: izinsiz import ve default export eksikliği LLM'e açıklanır.</summary>
    [Fact]
    public async Task Remotion_ValidateRejectsForbiddenImport()
    {
        var engine = new RemotionVideoEngine(new TestAgentPaths(Path.GetTempPath(), Path.Combine(Path.GetTempPath(), "tk-none")), new TestToolLocator(), new TestProcessRunner(), new FixedDurationProbe(1), new DefaultSceneCodeGuard(), NullLogger<RemotionVideoEngine>.Instance);
        var request = new Abstractions.Video.SceneRenderRequest(Path.GetTempPath(), 0, "import axios from 'axios';\nexport default function Scene() { return null; }", 1080, 1920, 30, 3, [], "x.mp4");

        var result = await engine.ValidateSceneAsync(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("axios");
    }

    /// <summary>Şablon package.json sabit sürümleri içerir ve deterministiktir.</summary>
    [Fact]
    public void HyperFrames_PackageJsonPinsVersions()
    {
        var json = CreateHyperFrames().BuildPackageJson();
        json.ShouldContain("\"hyperframes\": \"0.9.43\"");
        json.ShouldContain("\"gsap\": \"3.15.1\"");
        json.ShouldBe(CreateHyperFrames().BuildPackageJson());
    }

    private static HyperFramesVideoEngine CreateHyperFrames() =>
        new(Paths, new TestToolLocator(), new TestProcessRunner(), new FixedDurationProbe(1), new DefaultSceneCodeGuard(), NullLogger<HyperFramesVideoEngine>.Instance);
}
