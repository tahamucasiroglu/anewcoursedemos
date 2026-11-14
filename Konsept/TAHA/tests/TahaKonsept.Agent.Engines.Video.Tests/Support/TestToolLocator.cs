using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>
/// Test araç bulucu: ffmpeg/ffprobe <c>TAHAKONSEPT_FFMPEG_DIR</c> ya da <c>TAHA/.tools-dev/ffmpeg</c>; node PATH'ten.
/// npm ve Chrome bilerek <c>null</c> (motor kendi çözer/indirir).
/// </summary>
public sealed class TestToolLocator : IToolLocator
{
    /// <inheritdoc />
    public string? Find(ToolKind kind) => kind switch
    {
        ToolKind.Ffmpeg => FfmpegFile("ffmpeg"),
        ToolKind.Ffprobe => FfmpegFile("ffprobe"),
        ToolKind.Node => FindOnPath(OperatingSystem.IsWindows() ? "node.exe" : "node"),
        _ => null,
    };

    /// <inheritdoc />
    public Result<string> Require(ToolKind kind) =>
        Find(kind) is { } path ? path : Error.NotFound("tool.missing", $"{kind} bulunamadı.");

    private static string? FfmpegFile(string name)
    {
        var exe = OperatingSystem.IsWindows() ? name + ".exe" : name;
        var dir = Environment.GetEnvironmentVariable("TAHAKONSEPT_FFMPEG_DIR");
        if (string.IsNullOrWhiteSpace(dir) && TestEnvironment.RepoRoot is { } root)
        {
            dir = Path.Combine(root, ".tools-dev", "ffmpeg");
        }

        var candidate = dir is null ? null : Path.Combine(dir, exe);
        return candidate is not null && File.Exists(candidate) ? candidate : FindOnPath(exe);
    }

    private static string? FindOnPath(string exe) =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(d => Path.Combine(d.Trim('"'), exe))
            .FirstOrDefault(File.Exists);
}
