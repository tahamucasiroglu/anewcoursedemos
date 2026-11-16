using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.AiGateway.Contracts.Enums;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Dtos;
using Konsept.Modules.Concepts.Contracts.Enums;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Dtos;
using Konsept.Modules.Production.Application.Resources;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Konsept.Modules.Production.Domain.Rules;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Production.Application.Base;

/// <summary>
/// Üretim hattı (spec 0017). Kullanıcının çalışan işini aşama sırasıyla sürdürür: ajan bağlıysa önce araç denetimi (online çağrıdan önce,
/// Req 14), sonra analiz (tek çağrı) → sahne başına video kodu → görseller → seslendirme → render (hata → en fazla 1 düzeltme) → birleştirme.
/// Tamamlanmış aşama yeniden çalışmaz (çıktılar konsept metni özetiyle saklanır; yerel çıktılar yalnızca aynı ajan cihazında geçerli). Her
/// online çağrı ve aşamadan önce iş durumu yeniden okunur: Durdur'dan sonra yeni çağrı başlamaz, süren çağrı <c>CancellationToken.None</c> ile
/// tamamlanır ve maliyeti işe eklenir (Req 9). Ajan yoksa ya da koparsa iş Ajan bekleniyor olur (Req 15). İş bitince/hata olunca aynı
/// profilin sıradaki işi başlar; art arda 3 hatada sıra durur (Req 11). Ajana giden komutlarda key yoktur.
/// </summary>
/// <param name="jobs">İş deposu.</param>
/// <param name="settingsStore">Ayar deposu.</param>
/// <param name="outputsStore">Çıktı deposu.</param>
/// <param name="videos">Video kayıtları.</param>
/// <param name="queue">Sıra durumu.</param>
/// <param name="concepts">Konsept köprüsü.</param>
/// <param name="profiles">Profil okuyucu (Günlük etiketi).</param>
/// <param name="models">Model çağrısı (0013 kapısı).</param>
/// <param name="agents">Ajan komutları.</param>
/// <param name="components">Kurulu bileşenler (varsayılan ayarlar).</param>
/// <param name="events">Günlük.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Teknik log.</param>
public abstract class ProductionPipelineBase(
    IProductionJobRepository jobs,
    IProductionSettingsRepository settingsStore,
    IStageOutputRepository outputsStore,
    IProducedVideoRepository videos,
    IQueueStateRepository queue,
    IConceptQueueBridge concepts,
    IProfileGenerationReader profiles,
    IModelCaller models,
    IAgentCommands agents,
    IInstalledComponentsReader components,
    ISystemEventWriter events,
    TimeProvider time,
    ILogger logger) : IProductionPipeline
{
    /// <summary>Günlük olay türü.</summary>
    public const string EventType = "production.video";

    private static readonly ProductionJobStatus[] Producing = [ProductionJobStatus.Producing];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly Action<ILogger, Guid, Exception?> EventFailed = LoggerMessage.Define<Guid>(
        LogLevel.Warning, new EventId(5301, "ProductionEventFailed"), "Üretim işinin Günlük olayı yazılamadı: {JobId}");

    private static readonly Action<ILogger, Guid, string, Exception?> StageFailedLog = LoggerMessage.Define<Guid, string>(
        LogLevel.Warning, new EventId(5302, "ProductionStageFailed"), "Üretim aşaması hata verdi: {JobId} {Stage}");

    private static readonly Action<ILogger, Guid, Exception?> GpuFailedLog = LoggerMessage.Define<Guid>(
        LogLevel.Error, new EventId(5303, "ProductionGpuUnavailable"), "Yerel aşama ekran kartını kullanamadı (CPU'ya düşülmedi): {JobId}");

    private ProductionJob _job = null!;
    private FixedJobSettings _fix = null!;
    private ConceptContent _content = null!;
    private JobProgress _progress = new();
    private StageOutputs _outputs = null!;
    private ModelCallerIdentity _caller = null!;
    private Guid? _checkedDevice;
    private bool _lastNotice;
    private CancellationToken _stop;

    /// <inheritdoc />
    public async Task<bool> RunAsync(Guid userId, ModelCallerIdentity caller, CancellationToken stop)
    {
        ArgumentNullException.ThrowIfNull(caller);
        var active = await jobs.ListActiveAsync(userId, CancellationToken.None).ConfigureAwait(false);
        var job = active.FirstOrDefault(j => j.Status is ProductionJobStatus.Producing or ProductionJobStatus.WaitingForAgent);
        if (job?.FixedSettings is null)
        {
            return false;
        }

        if (job.Status == ProductionJobStatus.WaitingForAgent)
        {
            // Ajan gelince kendiliğinden devam (Req 15, 23).
            if (agents.ConnectedDevice(userId) is null
                || !await jobs.TransitionAsync(userId, job.Id, [ProductionJobStatus.WaitingForAgent], ProductionJobStatus.Producing, time.GetUtcNow(), CancellationToken.None).ConfigureAwait(false))
            {
                return false;
            }

            job = job with { Status = ProductionJobStatus.Producing };
        }

        (_job, _fix, _caller, _stop) = (job, job.FixedSettings, caller, stop);
        var outcome = await RunJobAsync().ConfigureAwait(false);
        switch (outcome)
        {
            case PipelineOutcome.Completed:
                await queue.SetAsync(new QueueState { UserId = userId, UpdatedAt = time.GetUtcNow() }, CancellationToken.None).ConfigureAwait(false);
                return await StartNextAsync(userId, job.ProfileId).ConfigureAwait(false);
            case PipelineOutcome.Failed:
                var previous = (await queue.GetAsync(userId, CancellationToken.None).ConfigureAwait(false))?.ConsecutiveFailures ?? 0;
                var (count, halt) = PipelineRules.AfterFailure(previous);
                await queue.SetAsync(new QueueState { UserId = userId, ConsecutiveFailures = count, Halted = halt, UpdatedAt = time.GetUtcNow() }, CancellationToken.None)
                    .ConfigureAwait(false);
                return !halt && await StartNextAsync(userId, job.ProfileId).ConfigureAwait(false);
            default:
                return false;
        }
    }

    private static PipelineStage? Next(JobProgress progress) =>
        PipelineRules.Order.Cast<PipelineStage?>().FirstOrDefault(s => progress.StateOf(s!.Value) is not (StageState.Done or StageState.Skipped));

    private static string StageName(PipelineStage stage) => stage switch
    {
        PipelineStage.Analysis => ProductionText.StageAnalysis,
        PipelineStage.Code => ProductionText.StageCode,
        PipelineStage.Images => ProductionText.StageImages,
        PipelineStage.Voice => ProductionText.StageVoice,
        PipelineStage.Render => ProductionText.StageRender,
        _ => ProductionText.StageMerge,
    };

    private static string ModelError(Error? error) =>
        error?.Code == ModelCallErrorCodes.NoKey ? ProductionText.PipelineNoKey : error?.Message ?? ProductionText.PipelineAnalysisInvalid;

    private static string Format(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

    private static string FileNameOf(string title, Guid videoId)
    {
        var slug = new StringBuilder();
        foreach (var c in title.Normalize(NormalizationForm.FormKC))
        {
            slug.Append(char.IsLetterOrDigit(c) ? c : '-');
        }

        var name = string.Join('-', slug.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        name = name.Length > 60 ? name[..60] : name;
        return (name.Length == 0 ? "video" : name) + "-" + videoId.ToString("N")[..8] + ".mp4";
    }

    private async Task<PipelineOutcome> RunJobAsync()
    {
        var now = time.GetUtcNow();
        var firstStart = _job.Progress?.StartedAt is null;
        _progress = (_job.Progress ?? new JobProgress()) with { StartedAt = _job.Progress?.StartedAt ?? now, EndedAt = null, Error = null };
        var content = await concepts.GetContentAsync(_job.UserId, _job.ConceptId, CancellationToken.None).ConfigureAwait(false);
        if (content is null)
        {
            _progress = _progress with { Stages = PipelineRules.Initial(_fix, null, null) };
            return await FailAsync(PipelineStage.Analysis, ProductionText.PipelineConceptMissing).ConfigureAwait(false);
        }

        _content = content;
        var hash = PipelineRules.Hash(JsonSerializer.Serialize(new { content.Title, content.Scenes }, Json));
        var stored = await outputsStore.GetAsync(_job.UserId, _job.ConceptId, CancellationToken.None).ConfigureAwait(false);
        _outputs = stored is not null && string.Equals(stored.TextHash, hash, StringComparison.Ordinal)
            ? stored
            : new StageOutputs { UserId = _job.UserId, ConceptId = _job.ConceptId, TextHash = hash, UpdatedAt = now }; // Metin değişti → baştan (Req 10).
        var device = agents.ConnectedDevice(_job.UserId);
        _progress = _progress with { Stages = PipelineRules.Initial(_fix, _outputs, device) };
        await SaveProgressAsync().ConfigureAwait(false);
        if (firstStart)
        {
            await WriteEventAsync(SystemEventLevel.Info, Format(ProductionText.EventStarted, _job.Title), null).ConfigureAwait(false);
        }

        // Ajan bağlıysa, hiçbir online çağrıdan önce araçlar sorulur (Req 14, AC-16).
        if (device is { } connected && (await CheckToolsAsync(connected).ConfigureAwait(false)).Error is { } toolError)
        {
            return await FailAsync(Next(_progress) ?? PipelineStage.Analysis, toolError).ConfigureAwait(false);
        }

        while (Next(_progress) is { } stage)
        {
            if (!await StillProducingAsync().ConfigureAwait(false))
            {
                return PipelineOutcome.Stopped;
            }

            if (PipelineRules.LocalStages.Contains(stage))
            {
                var current = agents.ConnectedDevice(_job.UserId);
                if (current is null)
                {
                    return await WaitAsync().ConfigureAwait(false);
                }

                if (_checkedDevice != current)
                {
                    // Yeni ajan (yeniden eşleştirme, Req 23): araçlar ona da sorulur.
                    var (dropped, error) = await CheckToolsAsync(current.Value).ConfigureAwait(false);
                    if (dropped)
                    {
                        return await WaitAsync().ConfigureAwait(false);
                    }

                    if (error is not null)
                    {
                        return await FailAsync(stage, error).ConfigureAwait(false);
                    }
                }

                if (_outputs.LocalDevice != current)
                {
                    // Önceki yerel çıktılar başka cihazda: yalnızca yerel aşamalar yeniden çalışır (Req 23).
                    var hadLocal = _outputs.LocalDone.Count > 0;
                    _outputs = _outputs with { LocalDevice = current, LocalDone = [] };
                    if (hadLocal)
                    {
                        _progress = _progress with { Stages = PipelineRules.Initial(_fix, _outputs, current) };
                        continue;
                    }
                }
            }

            _progress = _progress.With(stage, StageState.Running);
            await SaveProgressAsync().ConfigureAwait(false);
            var result = await RunStageAsync(stage).ConfigureAwait(false);
            switch (result.Outcome)
            {
                case PipelineOutcome.Continue:
                    _progress = _progress.With(stage, StageState.Done, result.Device);
                    if (stage is PipelineStage.Images or PipelineStage.Voice or PipelineStage.Render)
                    {
                        _outputs = _outputs with { LocalDone = [.. _outputs.LocalDone.Append(stage).Distinct()] };
                        await SaveOutputsAsync().ConfigureAwait(false);
                    }

                    await SaveProgressAsync().ConfigureAwait(false);
                    break;
                case PipelineOutcome.Failed:
                    return await FailAsync(stage, result.Error ?? Format(ProductionText.PipelineStageFailed, StageName(stage))).ConfigureAwait(false);
                case PipelineOutcome.Waiting:
                    _progress = _progress.With(stage, StageState.Pending);
                    return await WaitAsync().ConfigureAwait(false);
                case PipelineOutcome.Completed:
                    return PipelineOutcome.Completed;
                default:
                    _progress = _progress.With(stage, StageState.Pending);
                    await SaveProgressAsync().ConfigureAwait(false);
                    return PipelineOutcome.Stopped;
            }
        }

        return PipelineOutcome.Completed;
    }

    private Task<StageResult> RunStageAsync(PipelineStage stage) => stage switch
    {
        PipelineStage.Analysis => AnalyzeAsync(),
        PipelineStage.Code => WriteCodeAsync(),
        PipelineStage.Images => ImagesAsync(),
        PipelineStage.Voice => LocalAsync(AgentStageCommand.Voice, Scenes(code: false)),
        PipelineStage.Render => RenderAsync(),
        _ => MergeAsync(),
    };

    private async Task<StageResult> AnalyzeAsync()
    {
        var reply = await CallTextAsync(
            ModelRole.VideoAnalysis,
            PipelinePrompts.AnalysisSystem(),
            PipelinePrompts.AnalysisPrompt(_content.Title, _fix.Language, _fix.Format, [.. _content.Scenes.Select(s => (s.Title, s.Visual, s.Narration, s.DurationSeconds))]))
            .ConfigureAwait(false);
        if (!reply.IsSuccess)
        {
            return new StageResult(PipelineOutcome.Failed, ModelError(reply.Error));
        }

        var plan = reply.Value.Truncated ? null : PlanParser.ParsePlan(reply.Value.Text, [.. _content.Scenes.Select(s => (s.Narration, s.Visual, s.DurationSeconds))]);
        if (plan is null)
        {
            return new StageResult(PipelineOutcome.Failed, ProductionText.PipelineAnalysisInvalid);
        }

        _outputs = _outputs with { Plan = plan, Codes = null };
        await SaveOutputsAsync().ConfigureAwait(false);
        return new StageResult(PipelineOutcome.Continue);
    }

    private async Task<StageResult> WriteCodeAsync()
    {
        var plan = _outputs.Plan!;
        var codes = new List<string>(plan.Count);
        foreach (var scene in plan)
        {
            if (!await StillProducingAsync().ConfigureAwait(false))
            {
                return new StageResult(PipelineOutcome.Stopped); // Yarım aşama devamda baştan çalışır (Req 9).
            }

            var source = _content.Scenes[scene.Index];
            var reply = await CallTextAsync(
                ModelRole.VideoCode,
                PipelinePrompts.CodeSystem(_fix.Format, _fix.ImageModel is not null),
                PipelinePrompts.CodePrompt(scene, source.Title, source.Visual)).ConfigureAwait(false);
            if (!reply.IsSuccess)
            {
                return new StageResult(PipelineOutcome.Failed, ModelError(reply.Error));
            }

            if ((reply.Value.Truncated ? null : PlanParser.ParseCode(reply.Value.Text)) is not { } code)
            {
                return new StageResult(PipelineOutcome.Failed, ProductionText.PipelineCodeInvalid);
            }

            codes.Add(code);
        }

        _outputs = _outputs with { Codes = codes };
        await SaveOutputsAsync().ConfigureAwait(false);
        return new StageResult(PipelineOutcome.Continue);
    }

    private async Task<StageResult> ImagesAsync()
    {
        if (PipelineRules.IsLocal(_fix.ImageModel))
        {
            return await LocalAsync(AgentStageCommand.Images, Scenes(code: false)).ConfigureAwait(false);
        }

        // Online görsel: Pi'de kullanıcının key'iyle üretilir, sahne sahne yalnızca görsel ajana gider (key gitmez, Y-7).
        string? device = null;
        foreach (var scene in Scenes(code: false))
        {
            if (!await StillProducingAsync().ConfigureAwait(false))
            {
                return new StageResult(PipelineOutcome.Stopped);
            }

            var reply = await models.CallImageAsync(
                new ImageModelCall { Caller = _caller, ModelId = _fix.ImageModel!, ProfileId = _job.ProfileId, Prompt = scene.ImagePrompt ?? scene.Narration },
                CancellationToken.None).ConfigureAwait(false);
            if (reply.IsSuccess)
            {
                await AddCostAsync(reply.Value.CostUsd).ConfigureAwait(false);
            }

            if (!reply.IsSuccess || reply.Value.ImageBase64 is null)
            {
                return new StageResult(PipelineOutcome.Failed, reply.IsSuccess ? ProductionText.PipelineImageInvalid : ModelError(reply.Error));
            }

            var saved = await LocalAsync(AgentStageCommand.Images, [scene with { ImageBase64 = reply.Value.ImageBase64 }]).ConfigureAwait(false);
            if (saved.Outcome != PipelineOutcome.Continue)
            {
                return saved;
            }

            device = saved.Device;
        }

        return new StageResult(PipelineOutcome.Continue, Device: device);
    }

    private async Task<StageResult> RenderAsync()
    {
        while (true)
        {
            var (result, failedScene, detail) = await LocalReplyAsync(AgentStageCommand.Render, Scenes(code: true)).ConfigureAwait(false);
            if (failedScene is not { } index)
            {
                return result;
            }

            if (_progress.FixUsed)
            {
                return new StageResult(PipelineOutcome.Failed, ProductionText.PipelineRenderFailed); // İkinci düzeltme yok (Req 12, AC-13).
            }

            if (!await StillProducingAsync().ConfigureAwait(false))
            {
                return new StageResult(PipelineOutcome.Stopped);
            }

            _progress = _progress with { FixUsed = true };
            var codes = _outputs.Codes!.ToList();
            var reply = await CallTextAsync(
                ModelRole.CodeFix,
                PipelinePrompts.FixSystem(_fix.Format, _fix.ImageModel is not null),
                PipelinePrompts.FixPrompt(codes[index], detail ?? string.Empty)).ConfigureAwait(false);
            if (!reply.IsSuccess)
            {
                return new StageResult(PipelineOutcome.Failed, ModelError(reply.Error));
            }

            if ((reply.Value.Truncated ? null : PlanParser.ParseCode(reply.Value.Text)) is not { } code)
            {
                return new StageResult(PipelineOutcome.Failed, ProductionText.PipelineRenderFailed);
            }

            codes[index] = code;
            _outputs = _outputs with { Codes = codes };
            await SaveOutputsAsync().ConfigureAwait(false);
        }
    }

    private async Task<StageResult> MergeAsync()
    {
        var videoId = Guid.CreateVersion7(time.GetUtcNow());
        var fileName = FileNameOf(_job.Title, videoId);
        var (result, _, _) = await LocalReplyAsync(AgentStageCommand.Merge, Scenes(code: false), videoId, fileName).ConfigureAwait(false);
        if (result.Outcome != PipelineOutcome.Continue)
        {
            return result;
        }

        var now = time.GetUtcNow();
        _progress = _progress.With(PipelineStage.Merge, StageState.Done, result.Device) with { EndedAt = now };
        await SaveProgressAsync().ConfigureAwait(false);
        await videos.AddAsync(
            new ProducedVideo
            {
                Id = videoId,
                UserId = _job.UserId,
                ProfileId = _job.ProfileId,
                ConceptId = _job.ConceptId,
                JobId = _job.Id,
                Title = _job.Title,
                ConceptText = JsonSerializer.Serialize(_content, Json),
                CodeModel = _fix.CodeModel,
                ImageModel = _fix.ImageModel,
                AudioModel = _fix.AudioModel,
                Engine = _fix.Engine,
                Subtitles = _fix.Subtitles,
                ProgressBar = _fix.ProgressBar,
                AiVoiceNotice = _lastNotice,
                FileName = fileName,
                StartedAt = _progress.StartedAt ?? now,
                EndedAt = now,
            },
            CancellationToken.None).ConfigureAwait(false);
        await jobs.TransitionAsync(_job.UserId, _job.Id, Producing, ProductionJobStatus.Completed, now, CancellationToken.None).ConfigureAwait(false);
        await concepts.ChangeAsync(_job.UserId, _job.ConceptId, ConceptQueueStage.Producing, ConceptQueueStage.VideoReady, CancellationToken.None).ConfigureAwait(false);
        var start = _progress.StartedAt ?? now;
        await WriteEventAsync(
            SystemEventLevel.Info,
            Format(ProductionText.EventCompleted, _job.Title, JobTimeText.Duration(now - start), JobTimeText.Range(start, now)),
            now).ConfigureAwait(false);
        return new StageResult(PipelineOutcome.Completed);
    }

    private async Task<StageResult> LocalAsync(string stage, IReadOnlyList<AgentStageScene> scenes) =>
        (await LocalReplyAsync(stage, scenes).ConfigureAwait(false)).Result;

    private async Task<(StageResult Result, int? FailedScene, string? Detail)> LocalReplyAsync(
        string stage, IReadOnlyList<AgentStageScene> scenes, Guid? videoId = null, string? fileName = null)
    {
        var command = new AgentStageCommand
        {
            JobId = _job.Id,
            WorkKey = _job.ConceptId,
            Stage = stage,
            Subtitles = _fix.Subtitles,
            ProgressBar = _fix.ProgressBar,
            Engine = _fix.Engine,
            ImageModel = _fix.ImageModel,
            AudioModel = _fix.AudioModel,
            Language = _fix.Language,
            Format = _fix.Format,
            Scenes = scenes,
            VideoId = videoId,
            VideoFileName = fileName,
        };
        AgentStageReply? reply;
        try
        {
            reply = await agents.RunStageAsync(_job.UserId, command, _stop).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
            await agents.CancelStageAsync(_job.UserId, _job.Id).ConfigureAwait(false); // Durdur: ajandaki aşama da kesilir (Req 9).
            return (new StageResult(PipelineOutcome.Stopped), null, null);
        }

        if (reply is null)
        {
            return (new StageResult(PipelineOutcome.Waiting), null, null); // Ajan yok ya da koptu (Req 15).
        }

        if (reply.Ok)
        {
            _lastNotice = reply.AiVoiceNotice;
            return (new StageResult(PipelineOutcome.Continue, Device: reply.Device), null, null);
        }

        switch (reply.Error)
        {
            case AgentStageReply.Canceled:
                return (new StageResult(PipelineOutcome.Stopped), null, null);
            case AgentStageReply.RenderFailed when reply.FailedScene is { } scene && scene >= 0 && scene < scenes.Count:
                return (new StageResult(PipelineOutcome.Failed, ProductionText.PipelineRenderFailed), scene, reply.Detail);
            case AgentStageReply.ToolMissing:
                return (new StageResult(PipelineOutcome.Failed, Format(ProductionText.PipelineToolsMissing, PipelineRules.ToolName(reply.Detail ?? string.Empty))), null, null);
            case AgentStageReply.GpuUnavailable:
                GpuFailedLog(logger, _job.Id, null); // Req 14a: log Error, CPU'ya düşülmez.
                return (new StageResult(PipelineOutcome.Failed, Format(ProductionText.PipelineGpuUnavailable, PipelineRules.ToolName(reply.Detail ?? string.Empty))), null, null);
            default:
                StageFailedLog(logger, _job.Id, stage, null);
                return (new StageResult(PipelineOutcome.Failed), null, null);
        }
    }

    private List<AgentStageScene> Scenes(bool code) =>
        [.. _outputs.Plan!.Select(p => new AgentStageScene(p.Index, p.DurationSeconds, p.Narration, p.ImagePrompt, code ? _outputs.Codes?[p.Index] : null, null))];

    private async Task<(bool Dropped, string? Error)> CheckToolsAsync(Guid device)
    {
        var reply = await agents.CheckToolsAsync(_job.UserId, new AgentToolCheckRequest(PipelineRules.RequiredTools(_fix)), CancellationToken.None).ConfigureAwait(false);
        if (reply is null)
        {
            return (true, null); // Ajan koptu: yerel aşamada beklenir.
        }

        _checkedDevice = device;
        return (false, reply.Missing.Count == 0
            ? null
            : Format(ProductionText.PipelineToolsMissing, string.Join(", ", reply.Missing.Select(PipelineRules.ToolName))));
    }

    private async Task<Result<TextModelReply>> CallTextAsync(ModelRole role, string system, string prompt)
    {
        // Süren çağrı Durdur ile kesilmez (Req 9): tamamlanır, maliyeti kapıda kaydedilir ve işe eklenir.
        var reply = await models.CallTextAsync(
            new TextModelCall
            {
                Caller = _caller,
                ModelId = _fix.CodeModel ?? string.Empty,
                Role = role,
                ProfileId = _job.ProfileId,
                Effort = _fix.CodeEffort,
                System = system,
                Prompt = prompt,
            },
            CancellationToken.None).ConfigureAwait(false);
        if (reply.IsSuccess)
        {
            await AddCostAsync(reply.Value.CostUsd).ConfigureAwait(false);
        }

        return reply;
    }

    private async Task AddCostAsync(decimal cost)
    {
        _progress = _progress with { CostUsd = _progress.CostUsd + cost };
        await SaveProgressAsync().ConfigureAwait(false);
    }

    private async Task<bool> StillProducingAsync() =>
        (await jobs.GetAsync(_job.UserId, _job.Id, CancellationToken.None).ConfigureAwait(false))?.Status == ProductionJobStatus.Producing;

    private async Task<PipelineOutcome> WaitAsync()
    {
        await SaveProgressAsync().ConfigureAwait(false);
        return await jobs.TransitionAsync(_job.UserId, _job.Id, Producing, ProductionJobStatus.WaitingForAgent, time.GetUtcNow(), CancellationToken.None).ConfigureAwait(false)
            ? PipelineOutcome.Waiting
            : PipelineOutcome.Stopped;
    }

    private async Task<PipelineOutcome> FailAsync(PipelineStage stage, string error)
    {
        var now = time.GetUtcNow();
        _progress = _progress.With(stage, StageState.Failed) with { Error = error, EndedAt = now };
        await SaveProgressAsync().ConfigureAwait(false);
        if (!await jobs.TransitionAsync(_job.UserId, _job.Id, Producing, ProductionJobStatus.Failed, now, CancellationToken.None).ConfigureAwait(false))
        {
            return PipelineOutcome.Stopped; // Bu arada durduruldu.
        }

        var start = _progress.StartedAt ?? now;
        await WriteEventAsync(
            SystemEventLevel.Error,
            Format(ProductionText.EventFailed, _job.Title, JobTimeText.Duration(now - start), JobTimeText.Range(start, now), error),
            now).ConfigureAwait(false);
        return PipelineOutcome.Failed;
    }

    private Task SaveProgressAsync() =>
        jobs.SetProgressAsync(_job.UserId, _job.Id, _progress, time.GetUtcNow(), CancellationToken.None);

    private Task SaveOutputsAsync()
    {
        _outputs = _outputs with { UpdatedAt = time.GetUtcNow() };
        return outputsStore.UpsertAsync(_outputs, CancellationToken.None);
    }

    private async Task<bool> StartNextAsync(Guid userId, Guid profileId)
    {
        var next = (await jobs.ListActiveAsync(userId, CancellationToken.None).ConfigureAwait(false))
            .Where(j => j.ProfileId == profileId && j.Status == ProductionJobStatus.Queued)
            .OrderBy(j => j.Position)
            .FirstOrDefault();
        if (next is null)
        {
            return false;
        }

        var settings = await settingsStore.GetAsync(userId, profileId, CancellationToken.None).ConfigureAwait(false)
            ?? ProductionDefaults.For(userId, profileId, components.Get(userId));
        var now = time.GetUtcNow();
        if (!await jobs.StartAsync(userId, next.Id, [ProductionJobStatus.Queued], JobFixing.Fix(next, settings, now), now, CancellationToken.None).ConfigureAwait(false))
        {
            return false;
        }

        await concepts.ChangeAsync(userId, next.ConceptId, ConceptQueueStage.Queued, ConceptQueueStage.Producing, CancellationToken.None).ConfigureAwait(false);
        return true;
    }

    private async Task WriteEventAsync(SystemEventLevel level, string message, DateTimeOffset? endedAt)
    {
        try
        {
            var profile = await profiles.GetAsync(_job.UserId, _job.ProfileId, CancellationToken.None).ConfigureAwait(false);
            await events.WriteAsync(
                new SystemEventEntry(EventType, level, message, _job.UserId, _progress.StartedAt, endedAt, _job.ProfileId, profile?.Name),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EventFailed(logger, _job.Id, ex); // Günlük yazılamasa da üretim sürer.
        }
    }
}
