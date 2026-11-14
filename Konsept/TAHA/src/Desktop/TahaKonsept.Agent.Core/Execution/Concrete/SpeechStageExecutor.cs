using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Core.Audio;
using TahaKonsept.Agent.Core.Audio.Abstract;
using TahaKonsept.Agent.Core.Execution.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Concrete;

/// <summary>
/// <c>Speech</c> aşaması: seçilen <see cref="ISpeechEngine"/> ile sahne sahne <c>speech/scene-&lt;i&gt;.wav</c> üretir (var olanlar atlanır;
/// boş metinli sahneye kısa sessizlik), sonra <c>speech/speech.wav</c>'a birleştirir ve <see cref="SpeechStageResult"/> döner.
/// </summary>
public sealed class SpeechStageExecutor : StageExecutorBase<SpeechStagePayload>
{
    /// <summary>Boş metinli sahne için sessizlik süresi (sn).</summary>
    public const double EmptySegmentSilenceSec = 0.6;

    private readonly IEnumerable<ISpeechEngine> _engines;
    private readonly IAudioConcatenator _audio;

    /// <summary>Oluşturur.</summary>
    /// <param name="engines">Ses motorları.</param>
    /// <param name="audio">Ses birleştirici.</param>
    public SpeechStageExecutor(IEnumerable<ISpeechEngine> engines, IAudioConcatenator audio)
    {
        _engines = engines;
        _audio = audio;
    }

    /// <inheritdoc />
    public override PipelineStage Stage => PipelineStage.Speech;

    /// <inheritdoc />
    protected override string GetOutputDirectory(StageExecutionContext context, SpeechStagePayload payload) =>
        Path.Combine(context.JobDir, WorkspaceLayout.SpeechDir);

    /// <inheritdoc />
    protected override Result Validate(SpeechStagePayload payload, StageExecutionContext context) =>
        payload.Segments.Count == 0
            ? Error.Validation("agent.invalid_payload", "Seslendirilecek sahne yok.")
            : Result.Success();

    /// <inheritdoc />
    protected override async Task<Result<StageExecutionOutput>> ExecuteCoreAsync(StageExecutionContext context, SpeechStagePayload payload, CancellationToken ct)
    {
        var engine = SelectByKey(_engines, payload.EngineKey, e => e.Key);
        if (engine is null)
        {
            return Error.NotFound("agent.engine_not_found", "'" + payload.EngineKey + "' ses motoru bu bilgisayarda yok.");
        }

        string? referenceAudio = null;
        if (!string.IsNullOrWhiteSpace(payload.ReferenceAudioArtifactName))
        {
            referenceAudio = context.FindInput(payload.ReferenceAudioArtifactName);
            if (referenceAudio is null)
            {
                return Error.NotFound("agent.input_missing", "Referans ses dosyası indirilemedi: " + payload.ReferenceAudioArtifactName);
            }
        }

        var ready = await EnsureEngineReadyAsync(engine, context, 0, 5, ct);
        if (ready.IsFailure)
        {
            return ready.Error;
        }

        var directory = GetOutputDirectory(context, payload);
        var speed = payload.Speed > 0 ? payload.Speed : 1.0;
        var paths = new List<string>(payload.Segments.Count);
        var silent = new List<string>();
        for (var i = 0; i < payload.Segments.Count; i++)
        {
            var segment = payload.Segments[i];
            var finalPath = Path.Combine(directory, AgentArtifactNames.SceneAudio(segment.SceneIndex));
            paths.Add(finalPath);
            if (string.IsNullOrWhiteSpace(segment.SpokenText))
            {
                silent.Add(finalPath);
                continue;
            }

            context.Report(5 + (85d * i / payload.Segments.Count), "Seslendiriliyor (" + (i + 1) + "/" + payload.Segments.Count + ")");
            var produced = await RunCheckpointedAsync(finalPath, async partial =>
            {
                var result = await engine.SynthesizeAsync(
                    new SpeechRequest(segment.SpokenText, payload.Language, payload.VoiceId, referenceAudio, speed, partial),
                    ct);
                return result.Map(o => o.AudioPath);
            });
            if (produced.IsFailure)
            {
                return produced.Error;
            }
        }

        WriteSilences(paths, silent);
        context.Report(92, "Sesler birleştiriliyor…");
        return await CombineAsync(context, payload, paths, ct);
    }

    private static void WriteSilences(List<string> paths, List<string> silent)
    {
        if (silent.Count == 0)
        {
            return;
        }

        WavInfo? format = null;
        foreach (var path in paths.Except(silent))
        {
            if (WavFile.TryReadInfo(path, out var info))
            {
                format = info;
                break;
            }
        }

        foreach (var path in silent.Where(p => !IsComplete(p)))
        {
            WavFile.WriteSilence(path, EmptySegmentSilenceSec, format);
        }
    }

    private async Task<Result<StageExecutionOutput>> CombineAsync(StageExecutionContext context, SpeechStagePayload payload, List<string> paths, CancellationToken ct)
    {
        var artifacts = new List<ProducedArtifact>(paths.Count + 1);
        var timings = new List<SpeechSceneTiming>(paths.Count);
        var start = 0d;
        for (var i = 0; i < paths.Count; i++)
        {
            var duration = await _audio.GetDurationAsync(paths[i], ct);
            if (duration.IsFailure)
            {
                return duration.Error;
            }

            var sceneIndex = payload.Segments[i].SceneIndex;
            timings.Add(new SpeechSceneTiming
            {
                SceneIndex = sceneIndex,
                StartSec = start,
                DurationSec = duration.Value,
                FileName = Path.GetFileName(paths[i]),
            });
            artifacts.Add(CreateArtifact(context, paths[i], "audio", sceneIndex, duration.Value));
            start += duration.Value;
        }

        var combinedPath = WorkspaceLayout.SpeechAudioPath(context.JobDir);
        var combined = await _audio.ConcatenateAsync(paths, combinedPath, ct);
        if (combined.IsFailure)
        {
            return combined.Error;
        }

        artifacts.Add(CreateArtifact(context, combinedPath, "audio", null, combined.Value));
        context.Report(100, "Seslendirme hazır.");
        var result = new SpeechStageResult
        {
            AudioFileName = AgentArtifactNames.SpeechAudio,
            TotalDurationSec = combined.Value,
            Scenes = timings,
        };
        return new StageExecutionOutput(artifacts, Serialize(result));
    }
}
