using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Engines.Video.Media.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Media.Concrete;

/// <summary>ffprobe ile süre ve ses akışı okuyan <see cref="MediaProbeBase"/> uygulaması.</summary>
/// <param name="tools">Araç bulucu (ffprobe yolu).</param>
/// <param name="runner">Süreç çalıştırıcı.</param>
public sealed class FfprobeMediaProbe(IToolLocator tools, IProcessRunner runner) : MediaProbeBase
{
    /// <inheritdoc />
    protected override Task<Result<string>> ReadDurationTextAsync(string path, CancellationToken ct) =>
        RunAsync(["-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", path], ct);

    /// <inheritdoc />
    protected override Task<Result<string>> ReadAudioStreamsTextAsync(string path, CancellationToken ct) =>
        RunAsync(["-v", "error", "-select_streams", "a", "-show_entries", "stream=index", "-of", "csv=p=0", path], ct);

    private async Task<Result<string>> RunAsync(IReadOnlyList<string> args, CancellationToken ct)
    {
        var ffprobe = tools.Require(ToolKind.Ffprobe);
        if (ffprobe.IsFailure)
        {
            return Result.Failure<string>(ffprobe.Error);
        }

        var result = await runner.RunAsync(new ProcessSpec(ffprobe.Value, args, Timeout: TimeSpan.FromMinutes(1)), null, ct).ConfigureAwait(false);
        return result.Succeeded
            ? result.StandardOutput
            : Error.Failure("media.ffprobe_failed", $"ffprobe başarısız (kod {result.ExitCode}): {Tail(result.StandardError)}");
    }

    private static string Tail(string text) => text.Length <= 600 ? text.Trim() : text[^600..].Trim();
}
