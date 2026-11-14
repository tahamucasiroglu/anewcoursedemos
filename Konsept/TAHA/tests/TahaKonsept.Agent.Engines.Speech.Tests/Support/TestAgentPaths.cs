using TahaKonsept.Agent.Abstractions.Paths.Abstract;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>Testler için verilen kök altında klasör üreten <see cref="IAgentPaths"/>; ModelsDir ayrıca verilebilir.</summary>
public sealed class TestAgentPaths : IAgentPaths
{
    /// <summary>Kökü ve isteğe bağlı model klasörünü ayarlar.</summary>
    /// <param name="root">Kök klasör.</param>
    /// <param name="modelsDir">Model klasörü (yoksa kök/models).</param>
    public TestAgentPaths(string root, string? modelsDir = null)
    {
        RootDir = root;
        ModelsDir = modelsDir ?? Path.Combine(root, "models");
    }

    /// <inheritdoc />
    public string RootDir { get; }

    /// <inheritdoc />
    public string ToolsDir => Path.Combine(RootDir, "tools");

    /// <inheritdoc />
    public string ModelsDir { get; }

    /// <inheritdoc />
    public string PythonEnvsDir => Path.Combine(RootDir, "python-envs");

    /// <inheritdoc />
    public string WorkspaceDir => Path.Combine(RootDir, "workspace");

    /// <inheritdoc />
    public string VideosDir => Path.Combine(RootDir, "videos");

    /// <inheritdoc />
    public string LogsDir => Path.Combine(RootDir, "logs");

    /// <inheritdoc />
    public string ConfigDir => Path.Combine(RootDir, "config");

    /// <inheritdoc />
    public string GetJobDir(Guid jobId)
    {
        var dir = Path.Combine(WorkspaceDir, jobId.ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
