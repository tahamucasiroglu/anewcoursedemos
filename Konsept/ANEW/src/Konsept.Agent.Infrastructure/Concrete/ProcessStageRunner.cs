using System.Diagnostics;
using System.Globalization;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Base;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Contracts.Rules;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Yerel aşamaları kurulu araçlarla çalıştırır (spec 0017). Çalışma klasörü konsept başınadır (<c>jobs\&lt;konsept&gt;\scene-N</c>; yeniden
/// sıraya alınan iş aynı çıktıları kullanır). Online görsel Pi'den hazır gelir ve <c>image.png</c> olarak yazılır. Render (spec 0018): her sahne
/// kodu önce yasaklı yapı kapısından geçer (<see cref="RenderCodeGate"/>; ret → <see cref="AgentStageReply.CodeRejected"/>), sonra kısıtlı ortamda
/// (<see cref="ISceneRenderer"/>) render edilir; çıktı kapısından geçen sahnenin işareti (<see cref="SceneMarker"/>) devamda yeniden render'ı önler.
/// Birleştirme FFmpeg ile sabit şablondur (<see cref="MergeTemplate"/>); kodlayıcı bir kez yoklanır (B-117): NVENC varsa NVENC (kodlama sırasında
/// düşerse CPU'ya düşülmez, Req 14a), yoksa yazılım kodlayıcı <c>libopenh264</c> ve sahne klipleri WebM. Yerel görsel/ses modelleri bugün
/// kurulamaz (B-116): aşama "kurulu değil" döner. Süreç Durdur'da ağacıyla öldürülür.
/// </summary>
/// <param name="setup">Kurulum durumu.</param>
/// <param name="installRoot">Kurulum yeri.</param>
/// <param name="videoFolder">Video klasörünü veren kaynak.</param>
/// <param name="renderer">Kısıtlı sahne render'ı.</param>
/// <param name="logger">Teknik log.</param>
public sealed class ProcessStageRunner(ISetupStateStore setup, string installRoot, Func<string> videoFolder, ISceneRenderer renderer, ILogger logger)
    : StageRunnerBase(setup, logger)
{
    /// <summary>NVENC ile sahne klibi.</summary>
    public const string Mp4Clip = "clip.mp4";

    /// <summary>NVENC yokken sahne klibi (HyperFrames yazılım VP9).</summary>
    public const string WebmClip = "clip.webm";

    private Task<bool>? _nvenc;

    private string Ffmpeg => Path.Combine(Components, "ffmpeg", "bin", "ffmpeg.exe");

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

    /// <summary>
    /// NVENC kullanılabilir mi (B-117): ajan çalıştığı sürece bir kez, küçük bir deneme kodlamasıyla yoklanır. Kullanılamıyorsa yazılım
    /// kodlayıcıya geçilir (pinli LGPL FFmpeg'de libx264 yok; <c>libopenh264</c>).
    /// </summary>
    /// <returns>Kullanılabilirse <c>true</c>.</returns>
    public Task<bool> NvencAvailableAsync() =>
        _nvenc ??= ProbeNvencAsync();

    /// <inheritdoc />
    protected override async Task<AgentStageReply> RenderAsync(AgentStageCommand command, CancellationToken cancellationToken)
    {
        var engine = command.Engine.Replace("local/", string.Empty, StringComparison.Ordinal);
        foreach (var tool in new[] { engine, "node", "chromium", "ffmpeg" })
        {
            if (!IsInstalled(tool))
            {
                return Missing(tool);
            }
        }

        var nvenc = await NvencAvailableAsync().ConfigureAwait(false);
        var clipName = nvenc ? Mp4Clip : WebmClip;
        var (width, height) = MergeTemplate.Size(command.Format);
        var rendered = 0;
        foreach (var scene in command.Scenes)
        {
            var code = RenderCodeGate.Normalize(scene.Code ?? string.Empty);
            if (RenderCodeGate.Check(code) is { Ok: false } verdict)
            {
                Logger.Warning("Sahne kodu yasaklı yapı denetiminden geçmedi; render edilmedi: sahne {Scene}", scene.Index);
                return new AgentStageReply(false, AgentStageReply.CodeRejected, verdict.Reason, scene.Index);
            }

            var dir = SceneDir(command, scene.Index);
            var marker = SceneMarker.Of(code, command.Format, scene.DurationSeconds, clipName);
            var markerFile = Path.Combine(dir, SceneMarker.FileName);
            if (File.Exists(markerFile) && File.Exists(Path.Combine(dir, clipName))
                && string.Equals(await File.ReadAllTextAsync(markerFile, cancellationToken).ConfigureAwait(false), marker, StringComparison.Ordinal))
            {
                continue; // Tamamlanmış sahne yeniden render edilmez (Req 5, AC-7).
            }

            foreach (var stale in new[] { markerFile, Path.Combine(dir, Mp4Clip), Path.Combine(dir, WebmClip) })
            {
                File.Delete(stale);
            }

            await File.WriteAllTextAsync(Path.Combine(dir, "index.html"), RenderCsp.Apply(code), cancellationToken).ConfigureAwait(false); // L5: ajanın CSP etiketi (review r1 H-1).
            var result = await renderer.RenderAsync(new SceneRenderRequest(WorkDir(command), dir, width, height, scene.DurationSeconds, clipName), cancellationToken)
                .ConfigureAwait(false);
            switch (result.Outcome)
            {
                case SceneRenderOutcome.Ok:
                    await File.WriteAllTextAsync(markerFile, marker, cancellationToken).ConfigureAwait(false);
                    rendered++;
                    break;
                case SceneRenderOutcome.LimitExceeded:
                    return new AgentStageReply(false, AgentStageReply.RenderLimit, FailedScene: scene.Index);
                case SceneRenderOutcome.OutputInvalid:
                    return new AgentStageReply(false, AgentStageReply.OutputInvalid, result.Detail, scene.Index);
                case SceneRenderOutcome.Unsafe:
                    return new AgentStageReply(false, AgentStageReply.Failed);
                default:
                    Logger.Warning("Sahne render edilemedi: sahne {Scene}", scene.Index);
                    return new AgentStageReply(false, AgentStageReply.RenderFailed, result.Detail, scene.Index);
            }
        }

        Logger.Information("Render tamamlandı: {Rendered} sahne render edildi, {Kept} sahne korundu", rendered, command.Scenes.Count - rendered);
        return new AgentStageReply(true, Device: nvenc ? "gpu" : "cpu");
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
        var nvenc = await NvencAvailableAsync().ConfigureAwait(false);
        await File.WriteAllTextAsync(clips, MergeTemplate.ConcatList(command.Scenes.Select(s => ClipOf(SceneDir(command, s.Index)))), cancellationToken)
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
            Ffmpeg,
            MergeTemplate.Arguments(clips, audio, MergeTemplate.Filter(subtitles, command.ProgressBar, total, width), total, output, nvenc ? MergeTemplate.Encoder : MergeTemplate.SoftwareEncoder),
            work,
            cancellationToken).ConfigureAwait(false);
        if (exit != 0)
        {
            if (nvenc && MergeTemplate.IsGpuFailure(error))
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
        return new AgentStageReply(true, Device: nvenc ? "gpu" : "cpu", AiVoiceNotice: notice);
    }

    private static string ClipOf(string sceneDir) =>
        File.Exists(Path.Combine(sceneDir, WebmClip)) && !File.Exists(Path.Combine(sceneDir, Mp4Clip)) ? Path.Combine(sceneDir, WebmClip) : Path.Combine(sceneDir, Mp4Clip);

    private async Task<bool> ProbeNvencAsync()
    {
        if (!File.Exists(Ffmpeg))
        {
            return false;
        }

        var (exit, _) = await RunAsync(Ffmpeg, MergeTemplate.NvencProbeArguments, Components, CancellationToken.None).ConfigureAwait(false);
        Logger.Information("Video kodlayıcı: {Encoder}", exit == 0 ? MergeTemplate.Encoder : MergeTemplate.SoftwareEncoder);
        return exit == 0;
    }

    private static AgentStageReply Missing(string model) =>
        new(false, AgentStageReply.ToolMissing, model.Replace("local/", string.Empty, StringComparison.Ordinal));

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
