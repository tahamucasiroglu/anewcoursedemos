using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// <c>SceneCode</c> aşaması (sunucu, sahne başına LLM çağrısı): sistem promptu motorun yazım kılavuzudur (<see cref="IAuthoringGuideProvider"/>;
/// <c>{W}</c>/<c>{H}</c> doldurulur, <c>{D}</c> kullanıcı mesajındaki süreyle eşleşir; tüm sahnelerde aynı olduğu için önbelleğe alınır).
/// <see cref="IProviderGateway.RunTextAsync"/> ile üretilir (uzun sahnede 64000, diğerlerinde 32000 çıkış sınırı), <see cref="ISceneCodeValidator"/>
/// ile biçim denetlenir; geçersizse hata ile bir kez yeniden yazdırılır. Ajan lint'i reddederse orkestratör <c>RepairHint</c> ile yeniden çağırır.
/// Hash girdisi: planlanan sahne + video başlığı/dili/tonu + motor + model + efor + çözünürlük + fps (ses ayarları girmez).
/// </summary>
public sealed class SceneCodeStageHandler : StageHandlerBase
{
    /// <summary>Uzun sahne eşiği (sn); üstünde 64000 çıkış token'ı.</summary>
    public const double LongSceneSec = 40;

    private readonly IProviderGateway _gateway;
    private readonly IAuthoringGuideProvider _guides;
    private readonly ISceneCodeValidator _validator;
    private readonly ILogger<SceneCodeStageHandler> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="gateway">LLM geçidi.</param>
    /// <param name="guides">Yazım kılavuzları.</param>
    /// <param name="validator">Kod doğrulayıcı.</param>
    /// <param name="logger">Günlük.</param>
    public SceneCodeStageHandler(IProviderGateway gateway, IAuthoringGuideProvider guides, ISceneCodeValidator validator, ILogger<SceneCodeStageHandler> logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _guides = guides ?? throw new ArgumentNullException(nameof(guides));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Aşama.</summary>
    public override PipelineStage Stage => PipelineStage.SceneCode;

    /// <summary>Sunucu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Yer.</returns>
    public override StageLocation ResolveLocation(PipelineContext context) => StageLocation.Server;

    /// <summary>Hash girdisi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Girdi.</returns>
    public override object BuildHashInput(PipelineContext context, int? sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        var s = context.Settings;
        return new
        {
            scene = SceneOf(context, sceneIndex),
            title = context.Document.Title,
            language = context.Language,
            tone = context.Document.Tone,
            audience = context.Document.Audience,
            style = context.Document.NarrativeStyle,
            engine = s.VideoEngine,
            model = s.CodeModelKey,
            effort = s.CodeEffort,
            w = context.Width,
            h = context.Height,
            fps = context.Fps,
        };
    }

    /// <summary>Sahne kodunu üretir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override async Task<Result<StageExecutionResult>> ExecuteAsync(PipelineContext context, StageExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        var scene = SceneOf(context, request.SceneIndex);
        if (scene is null)
        {
            return ProductionErrors.MissingOutput(nameof(PipelineStage.ScenePlan));
        }

        var engine = context.Settings.VideoEngine.Trim().ToLowerInvariant();
        var guide = _guides.GetGuide(engine);
        if (guide is null)
        {
            return ProductionErrors.UnknownEngine(engine);
        }

        if (string.IsNullOrWhiteSpace(context.Settings.CodeModelKey))
        {
            return ProductionErrors.ModelMissing("Video kodu");
        }

        var system = BuildSystemPrompt(guide, context);
        var repair = request.RepairHint;
        decimal cost = 0;
        long inTokens = 0;
        long outTokens = 0;
        Error? lastError = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var task = new TextTask
            {
                UserId = context.UserId,
                ModelKey = context.Settings.CodeModelKey,
                Effort = context.Settings.CodeEffort,
                SystemPrompt = system,
                UserPrompt = BuildUserPrompt(context, scene, repair),
                MaxOutputTokens = scene.DurationSec > LongSceneSec ? 64000 : 32000,
                Purpose = "scene-code",
                CacheSystemPrompt = true,
            };
            var result = await _gateway.RunTextAsync(task, cancellationToken);
            if (result.IsFailure)
            {
                return result.Error;
            }

            cost += result.Value.CostUsd;
            inTokens += result.Value.Usage.TotalInputTokens;
            outTokens += result.Value.Usage.OutputTokens;
            request.Usage.Add(result.Value.CostUsd, result.Value.Usage.TotalInputTokens, result.Value.Usage.OutputTokens);
            var code = _validator.Extract(engine, result.Value.Value);
            if (code.IsSuccess)
            {
                var output = new SceneCodeOutput { SceneIndex = scene.Index, Engine = engine, Code = code.Value };
                return new StageExecutionResult
                {
                    OutputJson = ProductionJson.Write(output),
                    CostUsd = cost,
                    InputTokens = inTokens,
                    OutputTokens = outTokens,
                    Message = "Sahne " + (scene.Index + 1).ToString(CultureInfo.InvariantCulture) + " kodu yazıldı",
                };
            }

            lastError = code.Error;
            repair = code.Error.Message;
            _logger.LogWarning("Sahne kodu biçimi geçersiz (sahne {Scene}, deneme {Attempt}): {Error}", scene.Index, attempt + 1, code.Error.Message);
        }

        return lastError!;
    }

    private static string BuildSystemPrompt(string guide, PipelineContext context) =>
        guide.Replace("{W}", context.Width.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{H}", context.Height.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
        + "\n\n## Ek kurallar\n- `{D}` değeri kullanıcı mesajında verilen sahne süresidir (sn).\n- Kare hızı "
        + context.Fps.ToString(CultureInfo.InvariantCulture) + " fps.\n- Yalnızca sahne kodunu döndür; açıklama yazma.\n";

    private static string BuildUserPrompt(PipelineContext context, PlannedScene scene, string? repair)
    {
        var d = context.Document;
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.Append("Video: \"").Append(d.Title).Append("\" — ").Append(context.Width.ToString(inv)).Append('x').Append(context.Height.ToString(inv))
            .Append(", dil: ").Append(context.Language).Append('\n');
        if (!string.IsNullOrWhiteSpace(d.Tone) || !string.IsNullOrWhiteSpace(d.NarrativeStyle))
        {
            sb.Append("Ton: ").Append(d.Tone).Append(" | Anlatım: ").Append(d.NarrativeStyle).Append(" | Kitle: ").Append(d.Audience).Append('\n');
        }

        sb.Append("Sahne ").Append((scene.Index + 1).ToString(inv)).Append(" — süre {D} = ").Append(scene.DurationSec.ToString("0.##", inv)).Append(" sn\n");
        sb.Append("Başlık: ").Append(scene.Title).Append('\n');
        sb.Append("Ekranda gösterilecek: ").Append(scene.OnScreen).Append('\n');
        sb.Append("Seslendirme (altyazı sonradan eklenir; ekrana yazma, sadece görselleştir): ").Append(scene.Narration).Append('\n');
        if (!string.IsNullOrWhiteSpace(scene.VisualPrompt))
        {
            sb.Append("Görsel tarifi: ").Append(scene.VisualPrompt).Append('\n');
        }

        sb.Append(scene.ImageNames.Count > 0
            ? "Kullanılabilir görseller (yalnızca bunlar): " + string.Join(", ", scene.ImageNames.Select(n => "assets/" + n)) + "\n"
            : "Görsel dosyası yok: her şeyi koddan çiz (şekil, degrade, ikon, tipografi).\n");
        if (!string.IsNullOrWhiteSpace(repair))
        {
            sb.Append("\nÖnceki denemede şu doğrulama hatası alındı; düzelterek yeniden yaz:\n").Append(repair).Append('\n');
        }

        sb.Append("\nKuralları uygula ve yalnızca sahne kodunu döndür.");
        return sb.ToString();
    }
}
