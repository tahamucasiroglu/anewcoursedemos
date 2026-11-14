using System.Globalization;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>ScenePlan</c> aşaması (sunucu, deterministik): <see cref="IScenePlanner"/> ile sahne listesi, seslendirme uzunluğundan süreler ve görsel adları.
/// Hash girdisi: senaryo + bilgi özeti çıktı hash'leri ve görselin açık olup olmadığı (görsel modeli/ses ayarları plana girmez).
/// Ses açıksa seslendirme süresi seçili ses modelinin hızıyla (<see cref="ISpeechRateService"/>; yoksa planlayıcı sabiti) tahmin edilir;
/// profil hedefini <see cref="ProductionLimits.SpeechOverrunTolerance"/> oranından fazla aşarsa işlem günlüğüne uyarı yazılır (ses kesilmez).
/// </summary>
public sealed class ScenePlanStageHandler : StageHandlerBase
{
    private readonly IScenePlanner _planner;
    private readonly ISpeechRateService _rates;
    private readonly IProductionActivity _activity;

    /// <summary>Oluşturur.</summary>
    /// <param name="planner">Planlayıcı.</param>
    /// <param name="rates">Konuşma hızı servisi.</param>
    /// <param name="activity">İşlem günlüğü.</param>
    public ScenePlanStageHandler(IScenePlanner planner, ISpeechRateService rates, IProductionActivity activity)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _rates = rates ?? throw new ArgumentNullException(nameof(rates));
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.ScenePlan;

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
            script = context.Outputs.OutputHashOf(PipelineStage.Script),
            facts = context.Outputs.OutputHashOf(PipelineStage.FactSheet),
            images = context.Settings.ImageMode != MediaMode.Off,
        };
    }

    /// <summary>Planı üretir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override async Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var plan = _planner.Plan(context.Document, context.Settings.ImageMode != MediaMode.Off);
        await WarnIfSpeechTooLongAsync(context, plan, cancellationToken);
        return Output(plan, plan.Scenes.Count + " sahne, ~" + Math.Round(plan.TotalDurationSec) + " sn");
    }

    /// <summary>Tahmini seslendirme süresi profil hedefini %15'ten fazla aşıyorsa uyarı yazar.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="plan">Plan.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Tahmini süre (sn; ses kapalıysa <c>null</c>).</returns>
    public async Task<double?> WarnIfSpeechTooLongAsync(PipelineContext context, ScenePlanOutput plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(plan);
        var target = context.Document.TargetDurationSec;
        var words = plan.Scenes.Sum(s => NarrationWords.Count(s.Narration));
        if (context.Settings.SpeechMode == MediaMode.Off || target <= 0 || words == 0)
        {
            return null;
        }

        var rate = await _rates.GetWordsPerSecondAsync(context.UserId, context.Settings, context.Language, cancellationToken)
            ?? NarrationScenePlanner.WordsPerSecond;
        var estimated = Math.Round(words / rate, 1);
        if (estimated > target * (1 + ProductionLimits.SpeechOverrunTolerance))
        {
            var over = (int)Math.Round(((estimated / target) - 1) * 100);
            var inv = CultureInfo.InvariantCulture;
            var message = "Seslendirme profil hedefini aşıyor: tahmini ~" + Math.Round(estimated).ToString(inv) + " sn, hedef "
                + target.ToString(inv) + " sn (%" + over.ToString(inv) + " fazla; " + words.ToString(inv) + " kelime, "
                + (context.Settings.SpeechModelKey ?? "ses") + " ≈" + rate.ToString("0.0#", inv) + " kelime/sn). Ses kesilmez; konsepti kısaltmayı düşünün · "
                + context.Title;
            await _activity.WarningAsync(
                context.UserId,
                message,
                new { jobId = context.JobId, estimatedSec = estimated, targetSec = target, words, wordsPerSecond = rate, speechModel = context.Settings.SpeechModelKey },
                cancellationToken);
        }

        return estimated;
    }
}
