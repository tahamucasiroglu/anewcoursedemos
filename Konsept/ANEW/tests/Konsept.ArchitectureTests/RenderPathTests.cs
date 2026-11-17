using System.Text.RegularExpressions;
using Konsept.ArchitectureTests.Support;
using Shouldly;

namespace Konsept.ArchitectureTests;

/// <summary>
/// Spec 0020 AC-16 (H-4 ikinci kez açılmaz): yeni sürüm ve yeniden üretimin render'ı 0018'deki kısıtlı yoldan geçer, ayrı bir render yolu yoktur.
/// Kaynak taraması: ajana aşama komutu gönderen tek yer üretim hattıdır (<c>ProductionPipelineBase</c>); sürüm servisi ajana aşama komutu
/// göndermez, iş ekler; render komutu ajanda yalnızca aşama koşucusuna, oradan yalnızca kısıtlı sahne render'ına (<c>SandboxedSceneRenderer</c>
/// — tek <c>ISceneRenderer</c> uygulaması) gider.
/// </summary>
public sealed partial class RenderPathTests
{
    private const string Pipeline = "Konsept.Modules.Production.Application/Base/ProductionPipelineBase.cs";

    /// <summary>Ajana aşama komutu (render dahil) yalnızca hattan gider; sürüm servisi ve diğer Production kodu göndermez.</summary>
    [Fact]
    public void StageCommands_OnlyFromPipeline()
    {
        var callers = Files(RunStageCall()).Where(f => !f.StartsWith("Konsept.Modules.AgentGateway.", StringComparison.Ordinal)).ToList();
        callers.ShouldBe([Pipeline]);
        Files(RenderStage()).ShouldBe(["Konsept.Agent.Infrastructure/Base/StageRunnerBase.cs", Pipeline], ignoreOrder: true);
        var version = Read("Konsept.Modules.Production.Application/Base/VideoVersionServiceBase.cs");
        version.ShouldNotContain("RunStageAsync");
        version.ShouldNotContain("AgentStageCommand");
    }

    /// <summary>Ajanda render yalnızca aşama koşucusundan kısıtlı sahne render'ına gider; başka <c>ISceneRenderer</c> uygulaması yoktur.</summary>
    [Fact]
    public void AgentRender_OnlyThroughSandbox()
    {
        Files(SceneRenderCall()).ShouldBe(["Konsept.Agent.Infrastructure/Concrete/ProcessStageRunner.cs"]);
        Files(RendererImplementation()).ShouldBe(["Konsept.Agent.Infrastructure/Base/SceneRendererBase.cs"]);
        Files(RendererSubclass()).ShouldBe(["Konsept.Agent.Infrastructure/Concrete/SandboxedSceneRenderer.cs"]);
    }

    private static List<string> Files(Regex pattern)
    {
        var root = RepositoryPaths.SourceDirectory;
        return
        [
            .. Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Where(f => pattern.IsMatch(File.ReadAllText(f)))
                .Select(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'))
                .Order(StringComparer.Ordinal),
        ];
    }

    private static string Read(string relative) => File.ReadAllText(Path.Combine(RepositoryPaths.SourceDirectory, relative));

    [GeneratedRegex(@"\.RunStageAsync\(")]
    private static partial Regex RunStageCall();

    [GeneratedRegex(@"AgentStageCommand\.Render\b")]
    private static partial Regex RenderStage();

    [GeneratedRegex(@"\.RenderAsync\(new SceneRenderRequest")]
    private static partial Regex SceneRenderCall();

    [GeneratedRegex(@"class \w+[^\n]*:\s*[^\n]*\bISceneRenderer\b")]
    private static partial Regex RendererImplementation();

    [GeneratedRegex(@"class \w+[^\n]*:\s*SceneRendererBase\b")]
    private static partial Regex RendererSubclass();
}
