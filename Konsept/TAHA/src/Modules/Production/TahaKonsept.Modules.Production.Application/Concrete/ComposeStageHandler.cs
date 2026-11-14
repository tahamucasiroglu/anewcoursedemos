using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>Compose</c> aşaması (ajan): sahneleri birleştirir, ses varsa sesi ve karaoke altyazıyı, istenirse üst ilerleme çubuğunu basar
/// (<see cref="ComposeStagePayload"/>). Sahne süreleri gerçek ses zamanlarından (ses kapalıysa plandan) gelir; ajan kısa sahneyi son karede tutar.
/// Hash girdisi: tüm sahne render hash'leri, Speech/Align çıktı hash'leri, altyazı/çubuk seçenekleri, çözünürlük, fps, plan süreleri.
/// </summary>
public sealed class ComposeStageHandler : StageHandlerBase
{
    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.Compose;

    /// <summary>Ajan.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context) => StageLocation.Agent;

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Kullanılmaz.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scenes = context.Outputs.ScenePlan.Scenes;
        return new
        {
            renders = scenes.Select(s => context.Outputs.OutputHashOf(PipelineStage.SceneRender, s.Index)).ToList(),
            speech = context.Outputs.OutputHashOf(PipelineStage.Speech),
            align = context.Outputs.OutputHashOf(PipelineStage.Align),
            subtitles = context.Settings.BurnSubtitles,
            bar = context.Settings.ShowProgressBar,
            w = context.Width,
            h = context.Height,
            fps = context.Fps,
            durations = scenes.Select(s => s.DurationSec).ToList(),
        };
    }

    /// <summary>Birleştirme komutu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Komut.</returns>
    public override Task<Result<StageCommand>> BuildCommandAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var silent = IsSilent(context);
        var payload = new ComposeStagePayload
        {
            Width = context.Width,
            Height = context.Height,
            Fps = context.Fps,
            SceneDurationsSec = Durations(context),
            BurnSubtitles = context.Settings.BurnSubtitles && !silent,
            ShowProgressBar = context.Settings.ShowProgressBar,
            IncludeAudio = !silent,
        };
        return Task.FromResult(Result.Success(Command(context, request, payload, context.Outputs.ArtifactsOf(PipelineStage.Speech))));
    }

    /// <summary>Birleşik video süresi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="report">Rapor.</param>
    /// <returns>Sonuç.</returns>
    public override Result<StageExecutionResult> ApplyAgentResult(PipelineContext context, StageExecutionRequest request, StageResultReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        var durations = Durations(context);
        var output = new ComposeOutput { DurationSec = Math.Round(durations.Sum(), 2), SceneDurationsSec = durations };
        return Output(output, "Video birleştirildi (" + Math.Round(output.DurationSec) + " sn)");
    }

    /// <summary>Sahne süreleri: ses zamanları (yoksa plan).</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Süreler.</returns>
    public static IReadOnlyList<double> Durations(PipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var speech = context.Outputs.OutputOf<SpeechOutput>(PipelineStage.Speech);
        return context.Outputs.ScenePlan.Scenes
            .Select(s =>
            {
                var timing = speech?.Scenes.FirstOrDefault(t => t.SceneIndex == s.Index);
                return timing is not null && timing.DurationSec > 0 ? Math.Round(timing.DurationSec, 3) : s.DurationSec;
            })
            .ToList();
    }

    private static bool IsSilent(PipelineContext context) =>
        context.Settings.SpeechMode == MediaMode.Off || context.Outputs.OutputOf<SpeechOutput>(PipelineStage.Speech)?.Silent == true;
}
