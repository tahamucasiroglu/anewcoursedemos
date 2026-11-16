using System.Diagnostics;
using System.Globalization;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Base;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Yerel aşamaları kurulu araçlarla çalıştırır (spec 0017). Çalışma klasörü konsept başınadır (<c>jobs\&lt;konsept&gt;\scene-N</c>; yeniden
/// sıraya alınan iş aynı çıktıları kullanır). Online görsel Pi'den hazır gelir ve <c>image.png</c> olarak yazılır. Render HyperFrames CLI'si
/// ile (bileşen <c>components\hyperframes</c>; kurulumu ve çağrı biçiminin doğrulanması B-73/B-118, LLM kodunun kısıtlı ortamı 0018).
/// Birleştirme FFmpeg ile sabit şablondur (<see cref="MergeTemplate"/>): NVENC açılamazsa CPU'ya düşülmez (Req 14a). Yerel görsel/ses
/// modelleri bugün kurulamaz (B-116): aşama "kurulu değil" döner (araç denetimi zaten durdurur). Süreç Durdur'da ağacıyla öldürülür.
/// </summary>
/// <param name="setup">Kurulum durumu.</param>
/// <param name="installRoot">Kurulum yeri.</param>
/// <param name="videoFolder">Video klasörünü veren kaynak.</param>
/// <param name="logger">Teknik log.</param>
public sealed class ProcessStageRunner(ISetupStateStore setup, string installRoot, Func<string> videoFolder, ILogger logger)
    : StageRunnerBase(setup, logger)
{
    /// <summary>HyperFrames CLI'nin bileşen klasörüne göre yolu (B-118: kurulumla doğrulanacak).</summary>
    public const string HyperFramesCli = "node_modules/hyperframes/dist/cli.js";

    private string Components => Path.Combine(installRoot, "components");

    /// <inheritdoc />
    protected override async Task<AgentStageReply> ImagesAsync(AgentStageCommand command, CancellationToken cancellationToken)
    {
        if (command.ImageModel is { } model && model.StartsWith("local/", StringComparison.Ordinal))
        {
            return Missing(model); // Yerel görsel modeli koşucusu yok (B-116).
        }

        foreach (var scene in command.Scenes.Where(s => s.ImageBase64 is not null))
        {
            var dir = SceneDir(command, scene.Index);
            await File.WriteAllBytesAsync(Path.Combine(dir, "image.png"), Convert.FromBase64String(scene.ImageBase64!), cancellationToken).ConfigureAwait(false);
        }

        return new AgentStageReply(true, Device: "cpu");
    }

    /// <inheritdoc />
    protected override Task<AgentStageReply> VoiceAsync(AgentStageCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Missing(command.AudioModel ?? "audio")); // Yerel ses modeli koşucusu yok (B-116).

    /// <inheritdoc />
    protected override async Task<AgentStageReply> RenderAsync(AgentStageCommand command, CancellationToken cancellationToken)
    {
        var engine = command.Engine.Replace("local/", string.Empty, StringComparison.Ordinal);
        var cli = Path.Combine(Components, engine, HyperFramesCli);
        if (!IsInstalled(engine) || !File.Exists(cli))
        {
            return Missing(engine);
        }

        foreach (var scene in command.Scenes)
        {
            var dir = SceneDir(command, scene.Index);
            await File.WriteAllTextAsync(Path.Combine(dir, "index.html"), scene.Code ?? string.Empty, cancellationToken).ConfigureAwait(false);
            var (exit, error) = await RunAsync(
                Path.Combine(Components, "node", "node.exe"),
                [cli, "render", dir, "--output", Path.Combine(dir, "clip.mp4"), "--fps", "30"],
                dir,
                cancellationToken).ConfigureAwait(false);
            if (exit != 0)
            {
                Logger.Warning("Sahne render edilemedi: sahne {Scene}, çıkış {Exit}", scene.Index, exit);
                return new AgentStageReply(false, AgentStageReply.RenderFailed, Summary(error), scene.Index);
            }
        }

        return new AgentStageReply(true, Device: "gpu");
    }

    /// <inheritdoc />
    protected override async Task<AgentStageReply> MergeAsync(AgentStageCommand command, CancellationToken cancellationToken)
    {
        if (command.VideoFileName is not { } fileName || Path.GetFileName(fileName) != fileName)
        {
            return new AgentStageReply(false, AgentStageReply.Failed);
        }

        var work = WorkDir(command);
        var (width, height) = MergeTemplate.Size(command.Format);
        var total = command.Scenes.Sum(s => (double)s.DurationSeconds);
        var clips = Path.Combine(work, "clips.txt");
        await File.WriteAllTextAsync(clips, MergeTemplate.ConcatList(command.Scenes.Select(s => Path.Combine(SceneDir(command, s.Index), "clip.mp4"))), cancellationToken)
            .ConfigureAwait(false);
        string? audio = null;
        if (command.AudioModel is not null)
        {
            audio = Path.Combine(work, "voice.txt");
            await File.WriteAllTextAsync(audio, MergeTemplate.ConcatList(command.Scenes.Select(s => Path.Combine(SceneDir(command, s.Index), "voice.wav"))), cancellationToken)
                .ConfigureAwait(false);
        }

        var subtitles = command.Subtitles && command.AudioModel is not null; // Sessiz videoda altyazı yok (Req 4).
        if (subtitles)
        {
            await File.WriteAllTextAsync(
                Path.Combine(work, MergeTemplate.SubtitleFile),
                MergeTemplate.Subtitles([.. command.Scenes.Select(s => ((double)s.DurationSeconds, s.Narration))], width, height),
                cancellationToken).ConfigureAwait(false);
        }

        var folder = videoFolder();
        Directory.CreateDirectory(folder);
        var output = Path.Combine(folder, fileName);
        var (exit, error) = await RunAsync(
            Path.Combine(Components, "ffmpeg", "bin", "ffmpeg.exe"),
            MergeTemplate.Arguments(clips, audio, MergeTemplate.Filter(subtitles, command.ProgressBar, total, width), total, output),
            work,
            cancellationToken).ConfigureAwait(false);
        if (exit != 0)
        {
            if (MergeTemplate.IsGpuFailure(error))
            {
                Logger.Error("Video kodlaması ekran kartında yapılamadı; CPU'ya düşülmedi (Req 14a)");
                return new AgentStageReply(false, AgentStageReply.GpuUnavailable, "ffmpeg");
            }

            Logger.Warning("Birleştirme başarısız: çıkış {Exit}", exit);
            return new AgentStageReply(false, AgentStageReply.Failed);
        }

        var notice = AiVoiceNotice.Applies(command.AudioModel);
        if (notice)
        {
            await File.WriteAllTextAsync(AiVoiceNotice.FileFor(output), AiVoiceNotice.Text, cancellationToken).ConfigureAwait(false);
        }

        Logger.Information("Video hazırlandı: {File}", fileName);
        return new AgentStageReply(true, Device: "gpu", AiVoiceNotice: notice);
    }

    private static AgentStageReply Missing(string model) =>
        new(false, AgentStageReply.ToolMissing, model.Replace("local/", string.Empty, StringComparison.Ordinal));

    private static string Summary(string error)
    {
        var line = error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? "render hatası";
        return line.Length > 400 ? line[..400] : line;
    }

    private static async Task<(int Exit, string Error)> RunAsync(string exe, IReadOnlyList<string> args, string workingDirectory, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(exe)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        info.Environment["HYPERFRAMES_NO_TELEMETRY"] = "1";
        info.Environment["DO_NOT_TRACK"] = "1";
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Süreç başlatılamadı.");
        var stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);
        _ = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return (process.ExitCode, await stderr.ConfigureAwait(false));
    }

    private string WorkDir(AgentStageCommand command)
    {
        var dir = Path.Combine(installRoot, "jobs", command.WorkKey.ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private string SceneDir(AgentStageCommand command, int index)
    {
        var dir = Path.Combine(WorkDir(command), "scene-" + index.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
