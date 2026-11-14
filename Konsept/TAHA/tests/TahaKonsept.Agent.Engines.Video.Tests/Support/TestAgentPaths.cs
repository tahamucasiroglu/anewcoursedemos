using TahaKonsept.Agent.Abstractions.Paths.Abstract;

namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>Test klasörleri: kök geçici klasör, ToolsDir ayrıca verilebilir (motor şablon önbelleği için).</summary>
/// <param name="root">Kök klasör.</param>
/// <param name="toolsDir">Araç klasörü (<c>null</c> = kök/tools).</param>
public sealed class TestAgentPaths(string root, string? toolsDir = null) : IAgentPaths
{
    /// <inheritdoc />
    public string RootDir => root;

    /// <inheritdoc />
    public string ToolsDir => toolsDir ?? Path.Combine(root, "tools");

    /// <inheritdoc />
    public string ModelsDir => Path.Combine(root, "models");

    /// <inheritdoc />
    public string PythonEnvsDir => Path.Combine(root, "python-envs");

    /// <inheritdoc />
    public string WorkspaceDir => Path.Combine(root, "workspace");

    /// <inheritdoc />
    public string VideosDir => Path.Combine(root, "videos");

    /// <inheritdoc />
    public string LogsDir => Path.Combine(root, "logs");

    /// <inheritdoc />
    public string ConfigDir => Path.Combine(root, "config");

    /// <inheritdoc />
    public string GetJobDir(Guid jobId)
    {
        var dir = Path.Combine(WorkspaceDir, jobId.ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
