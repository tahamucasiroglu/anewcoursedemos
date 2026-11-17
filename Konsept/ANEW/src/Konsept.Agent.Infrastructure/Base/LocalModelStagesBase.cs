using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Setup;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Serilog;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Yerel model aşamaları (spec 0028 Req 7–11) — tek mekanizma: modelin bileşen listesindeki çalıştırma şablonu (<see cref="ModelRun"/>) çözülür,
/// aracın (bağımlılık bileşeni) yürütülebilir dosyası çalıştırılır. Görsel: sahne başına bir süreç, <c>image.png</c>; aynı tarifle devamda yeniden
/// üretilmez. Ses: tüm sahneler tek süreçte (girdi JSON'u: metin + ham çıktı), sonra her sahne FFmpeg ile sahne süresine oturtulur ve konuşma
/// süresi yazılır. Süreçler çevrimdışı, kullanıcı site-packages'ı ve önbellekleri kapalı, ajan klasöründe çalışır. Ekran kartı/bellek hatası →
/// <see cref="AgentStageReply.GpuUnavailable"/> (CPU'ya düşülmez); diğer hata → <see cref="AgentStageReply.Failed"/> (çıktının sonu yalnızca logda).
/// </summary>
/// <param name="catalog">Çözülmüş bileşen listesi.</param>
/// <param name="setup">Kurulum durumu.</param>
/// <param name="installRoot">Kurulum yeri.</param>
/// <param name="processes">Süreç çalıştırıcı.</param>
/// <param name="logger">Teknik log.</param>
public abstract class LocalModelStagesBase(
    IReadOnlyList<SetupComponent> catalog,
    ISetupStateStore setup,
    string installRoot,
    IProcessRunner processes,
    ILogger logger) : ILocalModelStages
{
    /// <summary>FFmpeg ile ses oturtmanın en uzun süresi (sahne başına).</summary>
    public static readonly TimeSpan FitTimeout = TimeSpan.FromMinutes(2);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ILogger _logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForContext<LocalModelStagesBase>();

    /// <summary>Gets betiklerin çıkarıldığı klasör.</summary>
    protected string RunnersDirectory => Path.Combine(installRoot, "runners");

    private string ComponentsDirectory => Path.Combine(installRoot, "components");

    /// <inheritdoc />
    public async Task<AgentStageReply> ImagesAsync(AgentStageCommand command, Func<int, string> sceneDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(sceneDirectory);
        var id = ModelId(command.ImageModel);
        if (Resolve(id, ModelRun.Image) is not { } model)
        {
            return Missing(id);
        }

        var (width, height) = LocalModelRules.ImageSize(command.Format);
        var produced = 0;
        foreach (var scene in command.Scenes)
        {
            var dir = sceneDirectory(scene.Index);
            var prompt = string.IsNullOrWhiteSpace(scene.ImagePrompt) ? scene.Narration : scene.ImagePrompt;
            var marker = LocalModelRules.ImageMarker(id, prompt, width, height);
            var image = Path.Combine(dir, LocalModelRules.ImageFile);
            var markerFile = Path.Combine(dir, LocalModelRules.ImageMarkerFile);
            if (File.Exists(image) && await MarkerMatchesAsync(markerFile, marker, cancellationToken).ConfigureAwait(false))
            {
                continue; // Aynı tarifle üretilmiş görsel korunur (devam).
            }

            DeleteFiles(markerFile, image);
            var temp = Path.Combine(dir, "image.tmp.png");
            var values = model.Values(new Dictionary<string, string>
            {
                [ModelRunTemplate.Prompt] = prompt,
                [ModelRunTemplate.Output] = temp,
                [ModelRunTemplate.Width] = width.ToString(CultureInfo.InvariantCulture),
                [ModelRunTemplate.Height] = height.ToString(CultureInfo.InvariantCulture),
                [ModelRunTemplate.Seed] = LocalModelRules.Seed(scene.Index),
                [ModelRunTemplate.Language] = command.Language,
            });
            var result = await RunModelAsync(model, values, dir, cancellationToken).ConfigureAwait(false);
            if (Failure(model.Component, result, File.Exists(temp)) is { } failure)
            {
                DeleteFiles(temp);
                return failure;
            }

            File.Move(temp, image, overwrite: true);
            await File.WriteAllTextAsync(markerFile, marker, cancellationToken).ConfigureAwait(false);
            produced++;
        }

        _logger.Information("Yerel görsel tamamlandı: {Model}, {Produced} sahne üretildi, {Kept} sahne korundu", id, produced, command.Scenes.Count - produced);
        return new AgentStageReply(true, Device: "gpu");
    }

    /// <inheritdoc />
    public async Task<AgentStageReply> VoiceAsync(AgentStageCommand command, string workDirectory, Func<int, string> sceneDirectory, string ffmpeg, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(sceneDirectory);
        var id = ModelId(command.AudioModel);
        if (Resolve(id, ModelRun.Speech) is not { } model)
        {
            return Missing(id);
        }

        if (!File.Exists(ffmpeg))
        {
            return Missing("ffmpeg");
        }

        var pending = new List<(AgentStageScene Scene, string Dir, string Marker)>();
        foreach (var scene in command.Scenes)
        {
            var dir = sceneDirectory(scene.Index);
            var marker = LocalModelRules.VoiceMarker(id, command.Language, scene.DurationSeconds, scene.Narration);
            if (!File.Exists(Path.Combine(dir, VoiceFit.VoiceFile)) || !File.Exists(Path.Combine(dir, VoiceFit.SpeechFile))
                || !await MarkerMatchesAsync(Path.Combine(dir, VoiceFit.MarkerFile), marker, cancellationToken).ConfigureAwait(false))
            {
                DeleteFiles(Path.Combine(dir, VoiceFit.MarkerFile), Path.Combine(dir, VoiceFit.VoiceFile), Path.Combine(dir, VoiceFit.SpeechFile));
                pending.Add((scene, dir, marker));
            }
        }

        var spoken = pending.Where(p => !string.IsNullOrWhiteSpace(p.Scene.Narration)).ToList();
        var seconds = new Dictionary<int, double>();
        var device = "cpu";
        if (spoken.Count > 0)
        {
            var input = Path.Combine(workDirectory, "voice-input.json");
            var output = Path.Combine(workDirectory, "voice-output.json");
            DeleteFiles(output);
            var items = new JsonArray([.. spoken.Select(p => (JsonNode)new JsonObject
            {
                ["text"] = p.Scene.Narration,
                ["output"] = Path.Combine(p.Dir, VoiceFit.RawFile),
            })]);
            await File.WriteAllTextAsync(input, new JsonObject { ["items"] = items }.ToJsonString(Json), cancellationToken).ConfigureAwait(false);
            var values = model.Values(new Dictionary<string, string>
            {
                [ModelRunTemplate.Input] = input,
                [ModelRunTemplate.Output] = output,
                [ModelRunTemplate.Language] = command.Language,
            });
            var result = await RunModelAsync(model, values, workDirectory, cancellationToken).ConfigureAwait(false);
            if (Failure(model.Component, result, File.Exists(output)) is { } failure)
            {
                return failure;
            }

            var reply = JsonNode.Parse(await File.ReadAllTextAsync(output, cancellationToken).ConfigureAwait(false));
            device = reply?["device"]?.GetValue<string>() == "gpu" ? "gpu" : "cpu";
            var produced = reply?["items"]?.AsArray() ?? [];
            for (var i = 0; i < spoken.Count; i++)
            {
                var value = i < produced.Count ? produced[i]?["seconds"]?.GetValue<double>() : null;
                if (value is not > 0 || !File.Exists(Path.Combine(spoken[i].Dir, VoiceFit.RawFile)))
                {
                    _logger.Warning("Yerel ses modeli sahne sesini üretmedi: {Model} sahne {Scene}", id, spoken[i].Scene.Index);
                    return new AgentStageReply(false, AgentStageReply.Failed);
                }

                seconds[spoken[i].Scene.Index] = value.Value;
            }
        }

        foreach (var (scene, dir, marker) in pending)
        {
            var raw = Path.Combine(dir, VoiceFit.RawFile);
            var voice = Path.Combine(dir, VoiceFit.VoiceFile);
            var (tempo, speech) = seconds.TryGetValue(scene.Index, out var length) ? VoiceFit.Plan(length, scene.DurationSeconds) : (1d, 0d);
            var args = speech > 0 ? VoiceFit.Arguments(raw, voice, tempo, scene.DurationSeconds) : VoiceFit.SilenceArguments(voice, scene.DurationSeconds);
            var fit = await processes.RunAsync(new ProcessSpec(ffmpeg, args, dir, new Dictionary<string, string>(), FitTimeout), cancellationToken).ConfigureAwait(false);
            if (!fit.Ok || !File.Exists(voice))
            {
                _logger.Warning("Sahne sesi süresine oturtulamadı: sahne {Scene}, çıkış {Exit}", scene.Index, fit.ExitCode);
                return new AgentStageReply(false, AgentStageReply.Failed);
            }

            DeleteFiles(raw);
            await File.WriteAllTextAsync(Path.Combine(dir, VoiceFit.SpeechFile), speech.ToString("0.###", CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(dir, VoiceFit.MarkerFile), marker, cancellationToken).ConfigureAwait(false);
        }

        _logger.Information("Yerel ses tamamlandı: {Model}, {Produced} sahne üretildi, aygıt {Device}", id, pending.Count, device);
        return new AgentStageReply(true, Device: device);
    }

    /// <summary>Gömülü çalıştırma betiğini çıkarır (her çalıştırmada yeniden yazılır; içerik imzalı ajandan gelir).</summary>
    /// <param name="name">Betik adı (ör. <c>speech.py</c>).</param>
    /// <returns>Betiğin yolu.</returns>
    protected abstract string ExtractScript(string name);

    /// <summary>Model süreçlerinin ortam değişkenleri: çevrimdışı, önbellekler ajan klasöründe, kullanıcı site-packages'ı kapalı.</summary>
    /// <param name="workDirectory">Çalışma klasörü.</param>
    /// <returns>Değişkenler.</returns>
    protected Dictionary<string, string> Environment(string workDirectory)
    {
        var cache = Path.Combine(installRoot, "cache");
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HF_HUB_OFFLINE"] = "1",
            ["TRANSFORMERS_OFFLINE"] = "1",
            ["HF_HUB_DISABLE_TELEMETRY"] = "1",
            ["HF_HOME"] = Path.Combine(cache, "hf"),
            ["TORCH_HOME"] = Path.Combine(cache, "torch"),
            ["XDG_CACHE_HOME"] = cache,
            ["NUMBA_CACHE_DIR"] = Path.Combine(cache, "numba"),
            ["PYTHONNOUSERSITE"] = "1",
            ["PYTHONUTF8"] = "1",
            ["PYTHONDONTWRITEBYTECODE"] = "1",
            ["TMP"] = workDirectory,
            ["TEMP"] = workDirectory,
            ["DO_NOT_TRACK"] = "1",
        };
    }

    private static string ModelId(string? model) => (model ?? string.Empty).Replace("local/", string.Empty, StringComparison.Ordinal);

    private static AgentStageReply Missing(string id) => new(false, AgentStageReply.ToolMissing, id);

    private static async Task<bool> MarkerMatchesAsync(string markerFile, string marker, CancellationToken cancellationToken) =>
        File.Exists(markerFile) && string.Equals(await File.ReadAllTextAsync(markerFile, cancellationToken).ConfigureAwait(false), marker, StringComparison.Ordinal);

    private static void DeleteFiles(params string[] files)
    {
        foreach (var file in files)
        {
            File.Delete(file);
        }
    }

    /// <summary>Model bileşenini ve kapanışını çözer; listede yoksa, görevi uymuyorsa ya da kapanışta kurulu olmayan varsa <c>null</c>.</summary>
    private ResolvedModel? Resolve(string id, string task)
    {
        var component = catalog.FirstOrDefault(c => c.Id == id);
        if (component?.Run is not { } run || run.Task != task)
        {
            return null;
        }

        var installed = UsableComponents.Of(setup.Load(), catalog, installRoot);
        var closure = ComponentRules.Closure(id, catalog);
        if (!closure.All(installed.Contains) || catalog.FirstOrDefault(c => c.Id == run.Tool) is not { } tool || !closure.Contains(run.Tool))
        {
            return null;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal) { [ModelRunTemplate.Self] = Path.Combine(ComponentsDirectory, id) };
        foreach (var dependency in closure)
        {
            values[dependency] = Path.Combine(ComponentsDirectory, dependency);
        }

        foreach (var name in ModelRunTemplate.Names(run.Args).Where(n => n.StartsWith(ModelRunTemplate.ScriptPrefix, StringComparison.Ordinal)))
        {
            values[name] = ExtractScript(name[ModelRunTemplate.ScriptPrefix.Length..]);
        }

        return new ResolvedModel(component, Path.Combine(ComponentsDirectory, tool.Id, tool.Executable), values);
    }

    private async Task<ProcessResult> RunModelAsync(ResolvedModel model, IReadOnlyDictionary<string, string> values, string workDirectory, CancellationToken cancellationToken)
    {
        var run = model.Component.Run!;
        var args = ModelRunTemplate.Expand(run.Args, values);
        return await processes.RunAsync(
            new ProcessSpec(model.Executable, args, workDirectory, Environment(workDirectory), TimeSpan.FromSeconds(run.TimeoutSeconds)),
            cancellationToken).ConfigureAwait(false);
    }

    private AgentStageReply? Failure(SetupComponent component, ProcessResult result, bool outputExists)
    {
        if (result.Ok && outputExists)
        {
            return null;
        }

        var id = component.Id;
        var tail = result.Output.Length > 2000 ? result.Output[^2000..] : result.Output;

        // Bellek hatası ancak model ekran kartında çalışıyorsa ekran kartına yazılır (CPU modelinin RAM hatası değil; review r1 L-2).
        var onGpu = component.Gpu is not null || result.Output.Contains("cuda", StringComparison.OrdinalIgnoreCase);
        if (!result.TimedOut && onGpu && LocalModelRules.IsGpuFailure(result.Output))
        {
            _logger.Error("Yerel model ekran kartında çalışamadı; CPU'ya düşülmedi: {Model} (çıkış {Exit})", id, result.ExitCode);
            _logger.Warning("Yerel model çıktısının sonu: {Output}", tail);
            return new AgentStageReply(false, AgentStageReply.GpuUnavailable, id);
        }

        _logger.Warning("Yerel model çalışmadı: {Model} çıkış {Exit}, süre aşımı {TimedOut}; çıktının sonu: {Output}", id, result.ExitCode, result.TimedOut, tail);
        return new AgentStageReply(false, AgentStageReply.Failed);
    }
}
