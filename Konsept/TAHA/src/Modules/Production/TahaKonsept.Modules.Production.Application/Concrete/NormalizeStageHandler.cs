using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>Normalize</c> aşaması (sunucu, deterministik): her sahnenin seslendirmesi için yazılış (altyazı) ve okunuş (TTS) üretir —
/// <see cref="ISpeechTextNormalizer"/> kullanıcı sözlüğü (onaylı) + konsept önerileriyle. Seçili ses modeli için düzeltme kapalıysa okunuş = yazılış.
/// Hash girdisi: sahne seslendirmeleri, dil, sözlük, açık/kapalı (ses modeli/ses tonu girmez; ses değişince bu aşama yeniden kullanılır).
/// </summary>
public sealed class NormalizeStageHandler : StageHandlerBase
{
    private readonly ISpeechTextNormalizer _normalizer;

    /// <summary>Oluşturur.</summary>
    /// <param name="normalizer">Okunuş düzeltici.</param>
    public NormalizeStageHandler(ISpeechTextNormalizer normalizer)
    {
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
    }

    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.Normalize;

    /// <summary>Sunucu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context) => StageLocation.Server;

    /// <summary>Deterministik.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns><c>true</c>.</returns>
    public override bool IsDeterministic(PipelineContext context) => true;

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Kullanılmaz.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new
        {
            narrations = context.Outputs.ScenePlan.Scenes.Select(s => new { s.Index, s.Narration }).ToList(),
            language = context.Language,
            enabled = context.NormalizeEnabled,
            lexicon = context.NormalizeEnabled ? context.Lexicon : [],
        };
    }

    /// <summary>Okunuşları üretir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var enabled = context.NormalizeEnabled;
        var segments = new List<NormalizedSegment>();
        try
        {
            foreach (var scene in context.Outputs.ScenePlan.Scenes)
            {
                if (!enabled || string.IsNullOrWhiteSpace(scene.Narration))
                {
                    segments.Add(new NormalizedSegment { SceneIndex = scene.Index, DisplayText = scene.Narration, SpokenText = scene.Narration });
                    continue;
                }

                var normalized = _normalizer.Normalize(scene.Narration, context.Language, context.Lexicon);
                segments.Add(new NormalizedSegment
                {
                    SceneIndex = scene.Index,
                    DisplayText = string.IsNullOrEmpty(normalized.Display) ? scene.Narration : normalized.Display,
                    SpokenText = string.IsNullOrWhiteSpace(normalized.Spoken) ? scene.Narration : normalized.Spoken,
                    ChangeCount = normalized.Changes.Count,
                });
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            return Task.FromResult(Result.Failure<StageExecutionResult>(Error.Failure("production.normalize_failed", "Okunuş düzeltme başarısız: " + ex.Message)));
        }

        var changes = segments.Sum(s => s.ChangeCount);
        var output = new NormalizeOutput { Enabled = enabled, Segments = segments };
        return Task.FromResult(Output(output, enabled ? changes + " okunuş düzeltmesi" : "Okunuş düzeltme kapalı"));
    }
}
