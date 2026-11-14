using TahaKonsept.Agent.Abstractions.Paths.Abstract;

namespace TahaKonsept.Agent.Core.Paths.Base;

/// <summary>
/// Verilen kök altında ajan klasör düzenini kurar: <c>Tools</c>, <c>Models</c>, <c>PythonEnvs</c>, <c>Workspace</c>,
/// <c>Videos</c>, <c>Logs</c>, <c>Config</c>. Klasörler oluşturulurken yaratılır; <see cref="GetJobDir"/> iş klasörünü üretir.
/// </summary>
public abstract class AgentPathsBase : IAgentPaths
{
    /// <summary>Kökü ayarlar ve tüm klasörleri oluşturur.</summary>
    /// <param name="rootDir">Kök klasör.</param>
    protected AgentPathsBase(string rootDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDir);
        RootDir = Path.GetFullPath(rootDir);
        ToolsDir = Path.Combine(RootDir, "Tools");
        ModelsDir = Path.Combine(RootDir, "Models");
        PythonEnvsDir = Path.Combine(RootDir, "PythonEnvs");
        WorkspaceDir = Path.Combine(RootDir, "Workspace");
        VideosDir = Path.Combine(RootDir, "Videos");
        LogsDir = Path.Combine(RootDir, "Logs");
        ConfigDir = Path.Combine(RootDir, "Config");

        foreach (var dir in new[] { RootDir, ToolsDir, ModelsDir, PythonEnvsDir, WorkspaceDir, VideosDir, LogsDir, ConfigDir })
        {
            Directory.CreateDirectory(dir);
        }
    }

    /// <inheritdoc />
    public string RootDir { get; }

    /// <inheritdoc />
    public string ToolsDir { get; }

    /// <inheritdoc />
    public string ModelsDir { get; }

    /// <inheritdoc />
    public string PythonEnvsDir { get; }

    /// <inheritdoc />
    public string WorkspaceDir { get; }

    /// <inheritdoc />
    public string VideosDir { get; }

    /// <inheritdoc />
    public string LogsDir { get; }

    /// <inheritdoc />
    public string ConfigDir { get; }

    /// <inheritdoc />
    public string GetJobDir(Guid jobId)
    {
        var dir = Path.Combine(WorkspaceDir, jobId.ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
