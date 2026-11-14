using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Align.Abstract;
using TahaKonsept.Agent.Abstractions.Compose.Abstract;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Abstractions.Video.Abstract;
using TahaKonsept.Agent.Engines.Video.Authoring;
using TahaKonsept.Agent.Engines.Video.DependencyInjection;
using TahaKonsept.Agent.Engines.Video.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Unit;

/// <summary>DI kaydı ve gömülü yazım kılavuzları.</summary>
public sealed class RegistrationAndGuidesTests
{
    /// <summary>AddVideoEngines üç motoru, birleştiriciyi ve hizalayıcıyı çözer.</summary>
    [Fact]
    public void AddVideoEngines_ResolvesAllServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAgentPaths>(new TestAgentPaths(Path.GetTempPath()));
        services.AddSingleton<IToolLocator, TestToolLocator>();
        services.AddSingleton<IProcessRunner, TestProcessRunner>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddVideoEngines();
        services.AddVideoEngines(); // iki kez çağrı çift kayıt üretmez

        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        sp.GetServices<IVideoEngine>().Select(e => e.Key).ShouldBe(["hyperframes", "remotion", "revideo"]);
        sp.GetServices<IAgentEngine>().Count().ShouldBe(3);
        sp.GetRequiredService<IComposer>().ShouldNotBeNull();
        sp.GetServices<IAligner>().Single().Key.ShouldBe("proportional");
    }

    /// <summary>Kılavuzlar gömülü ve temel kuralları içeriyor.</summary>
    [Fact]
    public void Guides_AreEmbedded()
    {
        AuthoringGuides.HyperFrames.ShouldContain("window.__timelines");
        AuthoringGuides.HyperFrames.ShouldContain("lang=\"tr\"");
        AuthoringGuides.Remotion.ShouldContain("useCurrentFrame");
        AuthoringGuides.Revideo.ShouldContain("makeScene2D");
        AuthoringGuides.ForEngine("HyperFrames").ShouldBe(AuthoringGuides.HyperFrames);
        AuthoringGuides.ForEngine("yok").ShouldBeNull();
    }

    /// <summary>docs/engines kopyaları gömülü kaynakla aynı.</summary>
    [Fact]
    public void Guides_MatchDocsCopies()
    {
        if (TestEnvironment.RepoRoot is not { } root)
        {
            return;
        }

        var docs = Path.Combine(root, "docs", "engines");
        Normalize(File.ReadAllText(Path.Combine(docs, "hyperframes-authoring.md"))).ShouldBe(Normalize(AuthoringGuides.HyperFrames));
        Normalize(File.ReadAllText(Path.Combine(docs, "remotion-authoring.md"))).ShouldBe(Normalize(AuthoringGuides.Remotion));
        Normalize(File.ReadAllText(Path.Combine(docs, "revideo-authoring.md"))).ShouldBe(Normalize(AuthoringGuides.Revideo));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
