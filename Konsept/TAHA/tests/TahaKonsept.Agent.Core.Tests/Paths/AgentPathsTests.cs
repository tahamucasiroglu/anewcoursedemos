using Shouldly;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Concrete;
using TahaKonsept.Agent.Core.Paths.Concrete;
using TahaKonsept.Agent.Core.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Paths;

/// <summary><see cref="AgentPaths"/> ve <see cref="AgentSettingsStore"/> testleri.</summary>
public sealed class AgentPathsTests
{
    /// <summary>Tüm klasörler kök altında oluşturulur; iş klasörü N biçimli kimlikle üretilir.</summary>
    [Fact]
    public void Paths_CreatesLayoutUnderRoot()
    {
        using var temp = new TempDirectory();
        var paths = new AgentPaths(new AgentOptions { RootDirectory = temp.Path });

        foreach (var dir in new[] { paths.ToolsDir, paths.ModelsDir, paths.PythonEnvsDir, paths.WorkspaceDir, paths.VideosDir, paths.LogsDir, paths.ConfigDir })
        {
            Directory.Exists(dir).ShouldBeTrue(dir);
            Path.GetDirectoryName(dir).ShouldBe(paths.RootDir);
        }

        var jobId = Guid.NewGuid();
        var jobDir = paths.GetJobDir(jobId);
        Directory.Exists(jobDir).ShouldBeTrue();
        Path.GetFileName(jobDir).ShouldBe(jobId.ToString("N"));
        Path.GetDirectoryName(jobDir).ShouldBe(paths.WorkspaceDir);
    }

    /// <summary>Kök verilmezse <c>%LocalAppData%/TahaKonsept</c> kullanılır.</summary>
    [Fact]
    public void ResolveRoot_DefaultsToLocalAppData()
    {
        var root = AgentPaths.ResolveRoot(new AgentOptions());

        root.ShouldBe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TahaKonsept"));
    }

    /// <summary>Ayarlar ilk okumada tamamlanır, kalıcıdır ve akış anahtarı kurulum boyunca aynı kalır.</summary>
    [Fact]
    public void SettingsStore_GeneratesAndPersistsStreamKey()
    {
        using var temp = new TempDirectory();
        var options = new AgentOptions { RootDirectory = temp.Path, DefaultServerBaseUrl = "http://10.0.0.5:7111" };
        var paths = new AgentPaths(options);

        var first = new AgentSettingsStore(paths, options).Current;
        var second = new AgentSettingsStore(paths, options).Current;

        first.ServerBaseUrl.ShouldBe("http://10.0.0.5:7111");
        first.VideoServerPort.ShouldBe(7120);
        first.StreamKey.Length.ShouldBeGreaterThanOrEqualTo(40);
        first.InstallId.ShouldNotBe(Guid.Empty);
        second.ShouldBe(first);
        File.Exists(Path.Combine(paths.ConfigDir, AgentSettingsStore.FileName)).ShouldBeTrue();
    }

    /// <summary>Güncelleme kaydedilir ve olay tetiklenir.</summary>
    [Fact]
    public void SettingsStore_UpdatePersistsAndRaisesChanged()
    {
        using var temp = new TempDirectory();
        var options = new AgentOptions { RootDirectory = temp.Path };
        var paths = new AgentPaths(options);
        var store = new AgentSettingsStore(paths, options);
        AgentSettings? raised = null;
        store.Changed += (_, s) => raised = s;

        store.Update(s => s with { ServerBaseUrl = "http://pi.local:7111" });

        raised.ShouldNotBeNull();
        new AgentSettingsStore(paths, options).Current.ServerBaseUrl.ShouldBe("http://pi.local:7111");
    }
}
