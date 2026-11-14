using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Core.Tools.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Tools.Concrete;

/// <summary>
/// Eksik aracı ilgili <see cref="IPrerequisite"/> bileşeniyle kurar (kurulum ekranındaki "Kur" ile aynı kod):
/// uv → <c>uv</c>, Python → <c>python</c> (önce uv), Node/npm/npx → <c>node</c>, FFmpeg/FFprobe → <c>ffmpeg</c>,
/// Chrome Headless Shell → <c>chrome-headless-shell</c>. Eşlemesi olmayan araçlarda (sd.cpp, ComfyUI, nvidia-smi)
/// bulucunun "şu bileşeni kurun" hatasını döner. Önemli üye: <see cref="PrerequisiteIdFor"/>.
/// </summary>
public sealed class PrerequisiteToolProvisioner : ToolProvisionerBase
{
    private readonly IEnumerable<IPrerequisite> _prerequisites;

    /// <summary>Oluşturur.</summary>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="prerequisites">Ön gereksinim bileşenleri.</param>
    public PrerequisiteToolProvisioner(IToolLocator tools, IEnumerable<IPrerequisite> prerequisites)
        : base(tools)
    {
        _prerequisites = prerequisites;
    }

    /// <summary>Aracı kuran ön gereksinim kimliği; otomatik kurulamıyorsa <c>null</c>.</summary>
    /// <param name="kind">Araç.</param>
    /// <returns>Kimlik ya da <c>null</c>.</returns>
    public static string? PrerequisiteIdFor(ToolKind kind) => kind switch
    {
        ToolKind.Uv => "uv",
        ToolKind.Python => "python",
        ToolKind.Node or ToolKind.Npm or ToolKind.Npx => "node",
        ToolKind.Ffmpeg or ToolKind.Ffprobe => "ffmpeg",
        ToolKind.ChromeHeadlessShell => "chrome-headless-shell",
        _ => null,
    };

    /// <inheritdoc />
    protected override async Task<Result> InstallCoreAsync(ToolKind kind, IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        var id = PrerequisiteIdFor(kind);
        var prerequisite = id is null ? null : _prerequisites.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        if (prerequisite is null)
        {
            var missing = Tools.Require(kind);
            return missing.IsFailure ? Result.Failure(missing.Error) : Result.Success();
        }

        if (kind == ToolKind.Python && Tools.Find(ToolKind.Uv) is null)
        {
            // Gömülü Python uv ile kurulur. Kapı zaten tutulduğu için EnsureAsync değil, doğrudan bileşen kurulur.
            var uv = await InstallAsync("uv", progress, ct).ConfigureAwait(false);
            if (uv.IsFailure)
            {
                return uv;
            }
        }

        return await InstallAsync(prerequisite, progress, ct).ConfigureAwait(false);
    }

    private async Task<Result> InstallAsync(string id, IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        var prerequisite = _prerequisites.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        return prerequisite is null
            ? Error.NotFound("agent.prerequisite_missing", "'" + id + "' bileşeni bu ajanda kayıtlı değil.")
            : await InstallAsync(prerequisite, progress, ct).ConfigureAwait(false);
    }

    private static async Task<Result> InstallAsync(IPrerequisite prerequisite, IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new EngineSetupProgress(prerequisite.DisplayName + " kuruluyor…", null));
        return await prerequisite.InstallAsync(progress, ct).ConfigureAwait(false);
    }
}
