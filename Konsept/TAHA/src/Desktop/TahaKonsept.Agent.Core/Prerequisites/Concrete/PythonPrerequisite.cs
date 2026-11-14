using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Concrete;

/// <summary>
/// Gömülü Python 3.11: <c>uv python install 3.11</c> (<c>UV_PYTHON_INSTALL_DIR=Tools/python</c>) → <c>Tools/python/cpython-3.11…/python.exe</c>.
/// <see cref="UvPrerequisite"/>'ten sonra kurulur. İsteğe bağlı (Chatterbox ve diğer Python ses motorları; CUDA'lı torch motor venv'inde kurulur).
/// </summary>
public sealed class PythonPrerequisite : PrerequisiteBase
{
    /// <summary>Python sürümü.</summary>
    public const string PythonVersion = "3.11";

    private readonly IAgentPaths _paths;
    private readonly IToolLocator _tools;
    private readonly IProcessRunner _processes;

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="processes">Süreç çalıştırıcı.</param>
    public PythonPrerequisite(IAgentPaths paths, IToolLocator tools, IProcessRunner processes)
    {
        _paths = paths;
        _tools = tools;
        _processes = processes;
    }

    /// <inheritdoc />
    public override string Id => "python";

    /// <inheritdoc />
    public override string DisplayName => "Python " + PythonVersion;

    /// <inheritdoc />
    public override string Description => "Python tabanlı ses motorları (Chatterbox, XTTS, F5…) için gömülü Python.";

    /// <inheritdoc />
    public override bool IsOptional => true;

    /// <inheritdoc />
    public override string Group => PythonGroup;

    /// <inheritdoc />
    public override int Order => 50;

    /// <inheritdoc />
    public override string? InstalledVersion
    {
        get
        {
            var python = _tools.Find(ToolKind.Python);
            return python is null ? null : Path.GetFileName(Path.GetDirectoryName(python));
        }
    }

    /// <inheritdoc />
    public override bool IsInstalled() => _tools.Find(ToolKind.Python) is not null;

    /// <inheritdoc />
    public override async Task<Result> InstallAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        if (IsInstalled())
        {
            Report(progress, DisplayName + " zaten kurulu.", 100);
            return Result.Success();
        }

        var uv = _tools.Require(ToolKind.Uv);
        if (uv.IsFailure)
        {
            return Error.Failure("agent.prerequisite_dependency_missing", "Python kurulumu için önce 'uv' kurulmalı.");
        }

        var installDir = Path.Combine(_paths.ToolsDir, "python");
        Directory.CreateDirectory(installDir);
        Report(progress, DisplayName + " kuruluyor (uv)…", 5);
        var environment = new Dictionary<string, string>
        {
            ["UV_PYTHON_INSTALL_DIR"] = installDir,
            ["UV_CACHE_DIR"] = Path.Combine(_paths.ToolsDir, "_uv-cache"),
        };
        var result = await _processes.RunAsync(
            new ProcessSpec(uv.Value, ["python", "install", PythonVersion], _paths.ToolsDir, environment, TimeSpan.FromMinutes(20)),
            line => Report(progress, line, null),
            ct);
        if (!result.Succeeded)
        {
            return Error.Failure("agent.prerequisite_install_failed", "Python kurulamadı: " + (result.TimedOut ? "zaman aşımı" : result.StandardError.Trim()));
        }

        if (!IsInstalled())
        {
            return Error.Failure("agent.prerequisite_verify_failed", "Python kuruldu ama python.exe bulunamadı.");
        }

        Report(progress, DisplayName + " hazır.", 100);
        return Result.Success();
    }
}
