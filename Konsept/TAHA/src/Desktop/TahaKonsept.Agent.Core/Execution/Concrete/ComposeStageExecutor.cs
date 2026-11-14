using TahaKonsept.Agent.Abstractions.Compose;
using TahaKonsept.Agent.Abstractions.Compose.Abstract;
using TahaKonsept.Agent.Core.Execution.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Concrete;

/// <summary>
/// <c>Compose</c> aşaması: sahne videolarını, seslendirmeyi ve altyazı zamanlamasını (yerel ya da indirilen) toplayıp
/// <see cref="IComposer"/> ile <c>compose/composed.mp4</c> üretir; altyazı/ilerleme çubuğu seçenekleri girdiden gelir.
/// Küçük resim varsa <c>compose/thumb.jpg</c>'ye kopyalanır.
/// </summary>
public sealed class ComposeStageExecutor : StageExecutorBase<ComposeStagePayload>
{
    private readonly IEnumerable<IComposer> _composers;

    /// <summary>Oluşturur.</summary>
    /// <param name="composers">Birleştiriciler (ilk kayıt kullanılır).</param>
    public ComposeStageExecutor(IEnumerable<IComposer> composers)
    {
        _composers = composers;
    }

    /// <inheritdoc />
    public override PipelineStage Stage => PipelineStage.Compose;

    /// <inheritdoc />
    protected override string GetOutputDirectory(StageExecutionContext context, ComposeStagePayload payload) =>
        Path.Combine(context.JobDir, WorkspaceLayout.ComposeDir);

    /// <inheritdoc />
    protected override Result Validate(ComposeStagePayload payload, StageExecutionContext context) =>
        payload.SceneDurationsSec.Count == 0
            ? Error.Validation("agent.invalid_payload", "Birleştirilecek sahne yok.")
            : Result.Success();

    /// <inheritdoc />
    protected override async Task<Result<StageExecutionOutput>> ExecuteCoreAsync(StageExecutionContext context, ComposeStagePayload payload, CancellationToken ct)
    {
        var finalPath = WorkspaceLayout.ComposedVideoPath(context.JobDir);
        if (IsComplete(finalPath))
        {
            return new StageExecutionOutput([CreateArtifact(context, finalPath, "video", null, payload.SceneDurationsSec.Sum())]);
        }

        var composer = _composers.FirstOrDefault();
        if (composer is null)
        {
            return Error.NotFound("agent.composer_missing", "Bu bilgisayarda video birleştirici (FFmpeg) yok.");
        }

        var scenes = new List<string>(payload.SceneDurationsSec.Count);
        for (var i = 0; i < payload.SceneDurationsSec.Count; i++)
        {
            var local = WorkspaceLayout.SceneVideoPath(context.JobDir, i);
            var path = IsComplete(local) ? local : context.FindInput(AgentArtifactNames.SceneVideo(i));
            if (path is null)
            {
                return Error.NotFound("agent.input_missing", "Sahne videosu bulunamadı: " + AgentArtifactNames.SceneVideo(i));
            }

            scenes.Add(path);
        }

        string? audio = null;
        if (payload.IncludeAudio)
        {
            var localAudio = WorkspaceLayout.SpeechAudioPath(context.JobDir);
            audio = IsComplete(localAudio) ? localAudio : context.FindInput(AgentArtifactNames.SpeechAudio);
            if (audio is null)
            {
                return Error.NotFound("agent.input_missing", "Seslendirme dosyası bulunamadı.");
            }
        }

        var localCaptions = WorkspaceLayout.CaptionsPath(context.JobDir);
        var captions = AlignStageExecutor.ReadCaptionWords(IsComplete(localCaptions) ? localCaptions : context.FindInput(AgentArtifactNames.Captions));
        if (payload.BurnSubtitles && captions.Count == 0)
        {
            context.Report(0, "Altyazı zamanlaması bulunamadı; altyazısız birleştiriliyor.");
        }

        var workDir = Path.Combine(GetOutputDirectory(context, payload), "work");
        Directory.CreateDirectory(workDir);
        ComposeOutput? output = null;
        var progress = new InlineProgress<double>(p => context.Report(p, "Video birleştiriliyor…"));
        var produced = await RunCheckpointedAsync(finalPath, async partial =>
        {
            var result = await composer.ComposeAsync(
                new ComposeRequest(
                    workDir,
                    scenes,
                    payload.SceneDurationsSec,
                    audio,
                    captions,
                    payload.BurnSubtitles && captions.Count > 0,
                    payload.ShowProgressBar,
                    payload.Width,
                    payload.Height,
                    payload.Fps,
                    partial),
                progress,
                ct);
            if (result.IsSuccess)
            {
                output = result.Value;
            }

            return result.Map(o => o.VideoPath);
        });
        if (produced.IsFailure)
        {
            return produced.Error;
        }

        if (output?.ThumbnailPath is { } thumb && File.Exists(thumb))
        {
            File.Copy(thumb, WorkspaceLayout.ComposedThumbnailPath(context.JobDir), overwrite: true);
        }

        context.Report(100, "Video birleştirildi.");
        return new StageExecutionOutput([CreateArtifact(context, finalPath, "video", null, output?.DurationSec ?? payload.SceneDurationsSec.Sum())]);
    }
}
