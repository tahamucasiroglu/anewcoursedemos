using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Abstractions.Image.Abstract;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Abstractions.Video.Abstract;
using TahaKonsept.Agent.Core.Capabilities.Base;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Capabilities.Concrete;

/// <summary>
/// Yetenek algılayıcı: GPU için <c>nvidia-smi</c> (10 sn zaman aşımı), bileşenler için DI'daki tüm
/// <see cref="IVideoEngine"/>/<see cref="ISpeechEngine"/>/<see cref="IImageEngine"/> ve <see cref="IPrerequisite"/> örneklerinin kurulu olma durumu.
/// Görsel motorlarının desteklediği modeller ayrıca <see cref="ComponentKind.ImageModel"/> olarak eklenir.
/// </summary>
public sealed class CapabilityDetector : CapabilityDetectorBase
{
    private readonly IToolLocator _tools;
    private readonly IProcessRunner _processes;
    private readonly IAgentPaths _paths;
    private readonly IEnumerable<IVideoEngine> _videoEngines;
    private readonly IEnumerable<ISpeechEngine> _speechEngines;
    private readonly IEnumerable<IImageEngine> _imageEngines;
    private readonly IEnumerable<IPrerequisite> _prerequisites;
    private readonly ILogger<CapabilityDetector> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="processes">Süreç çalıştırıcı.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="videoEngines">Video motorları.</param>
    /// <param name="speechEngines">Ses motorları.</param>
    /// <param name="imageEngines">Görsel motorları.</param>
    /// <param name="prerequisites">Ön gereksinimler.</param>
    /// <param name="logger">Logger.</param>
    public CapabilityDetector(
        IToolLocator tools,
        IProcessRunner processes,
        IAgentPaths paths,
        IEnumerable<IVideoEngine> videoEngines,
        IEnumerable<ISpeechEngine> speechEngines,
        IEnumerable<IImageEngine> imageEngines,
        IEnumerable<IPrerequisite> prerequisites,
        ILogger<CapabilityDetector> logger)
    {
        _tools = tools;
        _processes = processes;
        _paths = paths;
        _videoEngines = videoEngines;
        _speechEngines = speechEngines;
        _imageEngines = imageEngines;
        _prerequisites = prerequisites;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<GpuInfo>> DetectGpusAsync(CancellationToken ct)
    {
        var smi = _tools.Find(ToolKind.NvidiaSmi);
        if (smi is null)
        {
            return [];
        }

        try
        {
            var result = await _processes.RunAsync(new ProcessSpec(smi, NvidiaSmiArguments, Timeout: TimeSpan.FromSeconds(10)), null, ct);
            return result.Succeeded ? ParseNvidiaSmi(result.StandardOutput) : [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "nvidia-smi çalıştırılamadı.");
            return [];
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyList<InstalledComponent> DetectComponents()
    {
        var components = new List<InstalledComponent>();
        foreach (var prerequisite in _prerequisites)
        {
            components.Add(new InstalledComponent
            {
                Id = prerequisite.Id,
                Kind = ComponentKind.Prerequisite,
                Version = prerequisite.InstalledVersion,
                IsReady = Safe(prerequisite.IsInstalled, prerequisite.Id),
            });
        }

        AddEngines(components, _videoEngines, ComponentKind.VideoEngine);
        AddEngines(components, _speechEngines, ComponentKind.SpeechEngine);
        AddEngines(components, _imageEngines, ComponentKind.ImageEngine);
        foreach (var engine in _imageEngines)
        {
            var ready = Safe(engine.IsInstalled, engine.Key);
            foreach (var model in engine.SupportedModels)
            {
                components.Add(new InstalledComponent { Id = model, Kind = ComponentKind.ImageModel, IsReady = ready, Note = engine.Key });
            }
        }

        return components;
    }

    /// <inheritdoc />
    protected override long DetectFreeDiskMb()
    {
        try
        {
            var root = Path.GetPathRoot(_paths.WorkspaceDir);
            return string.IsNullOrEmpty(root) ? 0 : new DriveInfo(root).AvailableFreeSpace / (1024 * 1024);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            return 0;
        }
    }

    private void AddEngines(List<InstalledComponent> components, IEnumerable<IAgentEngine> engines, ComponentKind kind)
    {
        foreach (var engine in engines)
        {
            components.Add(new InstalledComponent
            {
                Id = engine.Key,
                Kind = kind,
                IsReady = Safe(engine.IsInstalled, engine.Key),
                Note = engine.MinVramMb is { } vram ? "En az " + vram + " MB VRAM" : null,
            });
        }
    }

    private bool Safe(Func<bool> check, string id)
    {
        try
        {
            return check();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Component} kurulum durumu okunamadı.", id);
            return false;
        }
    }
}
