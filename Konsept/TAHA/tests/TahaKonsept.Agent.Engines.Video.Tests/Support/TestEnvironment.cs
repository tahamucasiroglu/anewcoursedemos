using TahaKonsept.Agent.Abstractions.Tools;

namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>
/// Test ortamı yardımcıları: depo kökü (TahaKonsept.slnx), araç varlığı, geçici klasörler.
/// Motor render testleri <c>TAHAKONSEPT_SKIP_ENGINE_TESTS=1</c> ile kapatılabilir; şablonlar <c>.tools-dev/agent-tools</c> altında önbelleklenir.
/// </summary>
public static class TestEnvironment
{
    /// <summary>Depo (TAHA) kökü; bulunamazsa <c>null</c>.</summary>
    public static string? RepoRoot { get; } = FindRepoRoot();

    /// <summary>ffmpeg ve ffprobe bulundu mu.</summary>
    public static bool HasFfmpeg
    {
        get
        {
            var tools = new TestToolLocator();
            return tools.Find(ToolKind.Ffmpeg) is not null && tools.Find(ToolKind.Ffprobe) is not null;
        }
    }

    /// <summary>Motor render testleri çalıştırılabilir mi (node + ffmpeg + .tools-dev var, kapatılmamış).</summary>
    public static bool CanRunEngines =>
        HasFfmpeg
        && new TestToolLocator().Find(ToolKind.Node) is not null
        && RepoRoot is not null
        && Directory.Exists(Path.Combine(RepoRoot, ".tools-dev"))
        && Environment.GetEnvironmentVariable("TAHAKONSEPT_SKIP_ENGINE_TESTS") != "1";

    /// <summary>Kalıcı motor şablon önbelleği (ToolsDir).</summary>
    public static string EngineToolsDir => Path.Combine(RepoRoot ?? Path.GetTempPath(), ".tools-dev", "agent-tools");

    /// <summary>Benzersiz geçici klasör oluşturur (ad boşluk ve tırnak içerir: filtre kaçışı da test edilsin).</summary>
    /// <param name="prefix">Önek.</param>
    /// <returns>Klasör yolu.</returns>
    public static string NewTempDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), "tk video tests", $"{prefix} it's,{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TahaKonsept.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName;
    }
}
