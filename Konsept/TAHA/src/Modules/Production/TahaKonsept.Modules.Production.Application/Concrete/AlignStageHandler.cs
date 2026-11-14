using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>Align</c> aşaması (ajan): seslendirme ile yazılış kelimelerini zamanlar (karaoke altyazı). Ses kapalıysa sunucuda boş geçer (altyazı yok).
/// Segmentler Normalize çıktısından (yazılış + okunuş) ve Speech sahne zamanlarından kurulur; online seste birleşik ses artefaktı eklenir.
/// Hash girdisi: Speech + Normalize çıktı hash'leri, hizalayıcı, dil.
/// </summary>
public sealed class AlignStageHandler : StageHandlerBase
{
    /// <summary>Varsayılan hizalayıcı.</summary>
    public const string DefaultAligner = "proportional";

    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.Align;

    /// <summary>Ses kapalıysa sunucu, değilse ajan.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Settings.SpeechMode == MediaMode.Off ? StageLocation.Server : StageLocation.Agent;
    }

    /// <summary>Ses kapalıyken deterministik.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Deterministikse <c>true</c>.</returns>
    public override bool IsDeterministic(PipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Settings.SpeechMode == MediaMode.Off;
    }

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Kullanılmaz.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Settings.SpeechMode == MediaMode.Off
            ? new { off = true }
            : new
            {
                speech = context.Outputs.OutputHashOf(PipelineStage.Speech),
                normalized = context.Outputs.OutputHashOf(PipelineStage.Normalize),
                aligner = DefaultAligner,
                language = context.Language,
            };
    }

    /// <summary>Ses kapalıyken boş zamanlama.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(Output(new AlignStageResult(), "Ses kapalı; hizalama yok"));

    /// <summary>Hizalama komutu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Komut.</returns>
    public override Task<Result<StageCommand>> BuildCommandAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var normalized = context.Outputs.OutputOf<NormalizeOutput>(PipelineStage.Normalize);
        var speech = context.Outputs.OutputOf<SpeechOutput>(PipelineStage.Speech);
        if (normalized is null || speech is null)
        {
            return Task.FromResult(Result.Failure<StageCommand>(ProductionErrors.MissingOutput(normalized is null ? nameof(PipelineStage.Normalize) : nameof(PipelineStage.Speech))));
        }

        var segments = normalized.Segments.Select(s =>
        {
            var timing = speech.Scenes.FirstOrDefault(t => t.SceneIndex == s.SceneIndex);
            return new AlignSegmentItem
            {
                SceneIndex = s.SceneIndex,
                DisplayText = s.DisplayText,
                SpokenText = s.SpokenText,
                StartSec = timing?.StartSec,
                EndSec = timing is null ? null : timing.StartSec + timing.DurationSec,
            };
        }).ToList();
        var payload = new AlignStagePayload
        {
            AlignerKey = DefaultAligner,
            Language = context.Language,
            AudioArtifactName = speech.AudioArtifactName,
            Segments = segments,
        };
        return Task.FromResult(Result.Success(Command(context, request, payload, context.Outputs.ArtifactsOf(PipelineStage.Speech))));
    }

    /// <summary>Kelime zamanlarını çıktı olarak saklar.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="report">Rapor.</param>
    /// <returns>Sonuç.</returns>
    public override Result<StageExecutionResult> ApplyAgentResult(PipelineContext context, StageExecutionRequest request, StageResultReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var result = ProductionJson.Read<AlignStageResult>(report.OutputJson) ?? new AlignStageResult();
        return Output(result, result.Words.Count + " kelime hizalandı");
    }
}
