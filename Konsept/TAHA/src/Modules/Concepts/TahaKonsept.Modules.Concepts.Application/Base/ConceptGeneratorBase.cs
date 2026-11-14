using System.Text.Encodings.Web;
using System.Text.Json;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Common;
using TahaKonsept.Modules.Concepts.Application.Llm;
using TahaKonsept.Modules.Concepts.Application.Mapping;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Common;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptGenerator"/> ortak davranışı: prompt → <c>IProviderGateway.RunStructuredAsync&lt;ConceptLlmOutput&gt;</c>
/// (sistem promptu önbellekli, gerçek bilgi açıksa web araması) → <see cref="BuildDocument"/> (temizlik, süre ipuçları, geçerli URL'ler,
/// tekrarsız okunuş/etiket). Çıkış bütçesi hedef kelimeye göre (<see cref="OutputBudget"/>).
/// </summary>
public abstract class ConceptGeneratorBase : IConceptGenerator
{
    /// <summary>Düzeltme promptundaki belge için kısa JSON ayarları (Türkçe karakterler kaçışsız → daha az token).</summary>
    protected static readonly JsonSerializerOptions PromptJson = new(ContractJson.Options)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    /// <summary>Oluşturur.</summary>
    /// <param name="gateway">LLM geçidi.</param>
    /// <param name="prompts">Prompt fabrikası.</param>
    protected ConceptGeneratorBase(IProviderGateway gateway, IConceptPromptFactory prompts)
    {
        Gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        Prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
    }

    /// <summary>LLM geçidi.</summary>
    protected IProviderGateway Gateway { get; }

    /// <summary>Prompt fabrikası.</summary>
    protected IConceptPromptFactory Prompts { get; }

    /// <summary>Konsept üretir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="brief">Girdi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public virtual Task<Result<GeneratedConcept>> GenerateAsync(ConceptGenerationContext context, ConceptBrief brief, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(brief);
        return RunAsync(context, Prompts.Concept(context, brief), "concept", null, cancellationToken);
    }

    /// <summary>Konsepti düzeltir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="document">Mevcut belge.</param>
    /// <param name="instruction">Talimat.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public virtual Task<Result<GeneratedConcept>> ReviseAsync(ConceptGenerationContext context, ConceptDocument document, string instruction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);
        var json = JsonSerializer.Serialize(document.ToLlm(), PromptJson);
        return RunAsync(context, Prompts.Revise(context, json, (instruction ?? string.Empty).Trim()), "concept-revise", document, cancellationToken);
    }

    /// <summary>Çıkış token bütçesi: 6000 + hedef kelime × 6, 8000–32000 arası (düşünme token'ları da sayılır).</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Token.</returns>
    protected virtual int OutputBudget(ConceptGenerationContext context) => Math.Clamp(6000 + (context.TargetWords * 6), 8000, 32000);

    /// <summary>LLM çıktısını belgeye çevirir (kanal alanları bağlamdan ya da şablon belgeden).</summary>
    /// <param name="output">LLM çıktısı.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="template">Şablon belge (düzeltmede mevcut belge).</param>
    /// <returns>Belge.</returns>
    protected virtual ConceptDocument BuildDocument(ConceptLlmOutput output, ConceptGenerationContext context, ConceptDocument? template)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(context);
        var scenes = (output.Scenes ?? [])
            .Where(s => s is not null && (!string.IsNullOrWhiteSpace(s.Narration) || !string.IsNullOrWhiteSpace(s.OnScreen)))
            .Take(ConceptLimits.MaxScenes)
            .Select((s, i) => new ConceptScene
            {
                Index = i,
                Title = Clean(s.Title),
                OnScreen = Clean(s.OnScreen),
                VisualPrompt = Clean(s.VisualPrompt),
                Narration = Clean(s.Narration),
                DurationHintSec = DurationHint(s, context),
            })
            .ToList();

        var title = Clean(output.Title);
        if (title.Length == 0)
        {
            title = scenes.FirstOrDefault()?.Title is { Length: > 0 } t ? t : "Adsız konsept";
        }

        return new ConceptDocument
        {
            Title = title.Length <= ConceptLimits.TitleMaxLength ? title : title[..ConceptLimits.TitleMaxLength],
            Hook = Clean(output.Hook),
            Summary = Clean(output.Summary),
            Language = template?.Language ?? context.Language,
            Format = template?.Format ?? context.Format,
            TargetDurationSec = template?.TargetDurationSec ?? context.TargetDurationSec,
            Tone = template?.Tone is { Length: > 0 } tone ? tone : context.Tone,
            Audience = template?.Audience is { Length: > 0 } audience ? audience : context.Audience,
            NarrativeStyle = template?.NarrativeStyle is { Length: > 0 } style ? style : context.NarrativeStyle,
            Scenes = scenes,
            Sources = (output.Sources ?? [])
                .Where(s => s is not null && IsHttpUrl(s.Url))
                .Select(s => new ConceptSource { Title = Clean(s.Title).Length > 0 ? Clean(s.Title) : s.Url.Trim(), Url = s.Url.Trim() })
                .DistinctBy(s => s.Url, StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList(),
            Pronunciations = (output.Pronunciations ?? [])
                .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Written) && !string.IsNullOrWhiteSpace(p.Spoken))
                .Select(p => new PronunciationEntry { Written = p.Written.Trim(), Spoken = p.Spoken.Trim() })
                .Where(p => !string.Equals(p.Written, p.Spoken, StringComparison.Ordinal))
                .DistinctBy(p => p.Written, StringComparer.OrdinalIgnoreCase)
                .Take(40)
                .ToList(),
            Tags = (output.Tags ?? [])
                .Select(t => (t ?? string.Empty).Trim().TrimStart('#').Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList(),
        };
    }

    private async Task<Result<GeneratedConcept>> RunAsync(ConceptGenerationContext context, PromptPair prompts, string purpose, ConceptDocument? template, CancellationToken cancellationToken)
    {
        var task = new TextTask
        {
            UserId = context.UserId,
            ModelKey = context.Preferences.ConceptModelKey,
            Effort = context.Preferences.ConceptEffort,
            SystemPrompt = prompts.System,
            UserPrompt = prompts.User,
            MaxOutputTokens = OutputBudget(context),
            UseWebSearch = context.FactGrounding,
            Purpose = purpose,
            CacheSystemPrompt = true,
        };

        var result = await Gateway.RunStructuredAsync<ConceptLlmOutput>(task, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var value = result.Value;
        var document = BuildDocument(value.Value, context, template);
        if (document.Scenes.Count == 0)
        {
            return ConceptErrors.InvalidModelOutput;
        }

        return new GeneratedConcept(
            document,
            string.IsNullOrEmpty(value.ModelKey) ? task.ModelKey : value.ModelKey,
            value.Usage.TotalInputTokens,
            value.Usage.OutputTokens,
            value.CostUsd,
            value.DurationMs);
    }

    private static double DurationHint(ConceptSceneLlm scene, ConceptGenerationContext context)
    {
        if (scene.DurationHintSec is > 0.5 and < 600)
        {
            return Math.Round(scene.DurationHintSec, 1);
        }

        var words = TurkishText.CountWords(scene.Narration);
        return Math.Round(Math.Max(1.5, words / Math.Max(0.5, context.WordsPerSecond)), 1);
    }

    private static bool IsHttpUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string Clean(string? text) => (text ?? string.Empty).Trim();
}
