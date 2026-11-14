using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Core.Tools.Base;

namespace TahaKonsept.Agent.Core.Tools.Concrete;

/// <summary>
/// Windows araç bulucu. Ön gereksinim yükleyicisinin kurduğu düzen: <c>Tools/node/node.exe</c>,
/// <c>Tools/ffmpeg/bin/ffmpeg.exe</c>, <c>Tools/chrome-headless-shell/chrome-headless-shell.exe</c>, <c>Tools/uv/uv.exe</c>,
/// <c>Tools/python/&lt;cpython-3.11…&gt;/python.exe</c>, <c>Tools/sd/sd.exe</c>, <c>Tools/comfyui/main.py</c>.
/// npm/npx için <c>npm-cli.js</c>/<c>npx-cli.js</c> döner (node ile çalıştırılır). Gömülü Python PATH'te aranmaz.
/// </summary>
public sealed class ToolLocator : ToolLocatorBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    public ToolLocator(IAgentPaths paths)
        : base(paths)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<string> GetLocalCandidates(ToolKind kind) => kind switch
    {
        ToolKind.Node => ["node/node.exe", "node/node"],
        ToolKind.Npm => ["node/node_modules/npm/bin/npm-cli.js"],
        ToolKind.Npx => ["node/node_modules/npm/bin/npx-cli.js"],
        ToolKind.Ffmpeg => ["ffmpeg/bin/ffmpeg.exe", "ffmpeg/ffmpeg.exe"],
        ToolKind.Ffprobe => ["ffmpeg/bin/ffprobe.exe", "ffmpeg/ffprobe.exe"],
        ToolKind.ChromeHeadlessShell => ["chrome-headless-shell/chrome-headless-shell.exe", "chrome-headless-shell/*/chrome-headless-shell.exe"],
        ToolKind.Uv => ["uv/uv.exe", "uv/*/uv.exe"],
        ToolKind.Python => ["python/*/python.exe", "python/python.exe"],
        ToolKind.StableDiffusionCpp => ["sd/sd.exe", "stable-diffusion-cpp/sd.exe", "sd/*/sd.exe"],
        ToolKind.ComfyUi => ["comfyui/main.py", "comfyui/ComfyUI/main.py"],
        ToolKind.NvidiaSmi => [],
        _ => [],
    };

    /// <inheritdoc />
    protected override IEnumerable<string> GetPathExecutableNames(ToolKind kind) => kind switch
    {
        ToolKind.Node => ["node.exe"],
        ToolKind.Ffmpeg => ["ffmpeg.exe"],
        ToolKind.Ffprobe => ["ffprobe.exe"],
        ToolKind.Uv => ["uv.exe"],
        ToolKind.NvidiaSmi => ["nvidia-smi.exe"],
        _ => [],
    };

    /// <inheritdoc />
    protected override IEnumerable<string> GetWellKnownLocations(ToolKind kind)
    {
        if (kind != ToolKind.NvidiaSmi)
        {
            return [];
        }

        var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return
        [
            Path.Combine(system, "nvidia-smi.exe"),
            Path.Combine(programFiles, "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe"),
        ];
    }

    /// <inheritdoc />
    protected override string? FindDerived(ToolKind kind)
    {
        if (kind is not (ToolKind.Npm or ToolKind.Npx))
        {
            return null;
        }

        var node = SearchPath("node.exe");
        if (node is null)
        {
            return null;
        }

        var script = kind == ToolKind.Npm ? "npm-cli.js" : "npx-cli.js";
        var candidate = Path.Combine(Path.GetDirectoryName(node) ?? string.Empty, "node_modules", "npm", "bin", script);
        return File.Exists(candidate) ? candidate : null;
    }

    /// <inheritdoc />
    protected override string GetMissingMessage(ToolKind kind) => kind switch
    {
        ToolKind.Node or ToolKind.Npm or ToolKind.Npx => "Node.js bulunamadı. Kurulum ekranından 'Node.js' bileşenini kurun.",
        ToolKind.Ffmpeg or ToolKind.Ffprobe => "FFmpeg bulunamadı. Kurulum ekranından 'FFmpeg' bileşenini kurun.",
        ToolKind.ChromeHeadlessShell => "Chrome Headless Shell bulunamadı. Kurulum ekranından 'Chrome Headless Shell' bileşenini kurun.",
        ToolKind.Uv => "uv bulunamadı. Kurulum ekranından 'uv' bileşenini kurun.",
        ToolKind.Python => "Gömülü Python bulunamadı. Kurulum ekranından 'Python 3.11' bileşenini kurun.",
        ToolKind.StableDiffusionCpp => "stable-diffusion.cpp bulunamadı. Görsel motorunu kurulum ekranından kurun.",
        ToolKind.ComfyUi => "ComfyUI bulunamadı. Kurulum ekranından ComfyUI bileşenini kurun.",
        ToolKind.NvidiaSmi => "nvidia-smi bulunamadı (NVIDIA sürücüsü kurulu değil olabilir).",
        _ => "Araç bulunamadı: " + kind,
    };
}
