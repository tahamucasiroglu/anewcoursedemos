using TahaKonsept.Agent.Abstractions.Paths.Abstract;

namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Testler için sabit klasörlü <see cref="IAgentPaths"/>.</summary>
public sealed class FakeAgentPaths : IAgentPaths
{
    /// <summary>Kök klasörden türetilmiş yollar.</summary>
    /// <param name="root">Kök.</param>
    /// <param name="toolsDir">Araç klasörü (varsayılan: root/tools).</param>
    /// <param name="modelsDir">Model klasörü (varsayılan: root/models).</param>
    public FakeAgentPaths(string root, string? toolsDir = null, string? modelsDir = null)
    {
        RootDir = root;
        ToolsDir = toolsDir ?? Path.Combine(root, "tools");
        ModelsDir = modelsDir ?? Path.Combine(root, "models");
    }

    /// <inheritdoc />
    public string RootDir { get; }

    /// <inheritdoc />
    public string ToolsDir { get; }

    /// <inheritdoc />
    public string ModelsDir { get; }

    /// <inheritdoc />
    public string PythonEnvsDir => Path.Combine(RootDir, "python");

    /// <inheritdoc />
    public string WorkspaceDir => Path.Combine(RootDir, "work");

    /// <inheritdoc />
    public string VideosDir => Path.Combine(RootDir, "videos");

    /// <inheritdoc />
    public string LogsDir => Path.Combine(RootDir, "logs");

    /// <inheritdoc />
    public string ConfigDir => Path.Combine(RootDir, "config");

    /// <inheritdoc />
    public string GetJobDir(Guid jobId) => Directory.CreateDirectory(Path.Combine(WorkspaceDir, jobId.ToString("N"))).FullName;
}
