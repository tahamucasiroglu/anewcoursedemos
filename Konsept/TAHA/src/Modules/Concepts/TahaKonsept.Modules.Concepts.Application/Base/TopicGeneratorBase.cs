using System.Text.RegularExpressions;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Llm;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="ITopicGenerator"/> ortak davranışı: istenenden biraz fazla başlık ister (eleme payı), numara/tırnakları temizler,
/// hafızadaki ve kendi içindeki tekrarları eler, ilk <c>count</c> tanesini döner. Çıkış bütçesi adet başına ~60 token.
/// </summary>
public abstract partial class TopicGeneratorBase : ITopicGenerator
{
    /// <summary>Oluşturur.</summary>
    /// <param name="gateway">LLM geçidi.</param>
    /// <param name="prompts">Prompt fabrikası.</param>
    /// <param name="memory">Konu hafızası.</param>
    /// <param name="normalizer">Normalleştirici.</param>
    protected TopicGeneratorBase(IProviderGateway gateway, IConceptPromptFactory prompts, ITopicMemory memory, ITopicNormalizer normalizer)
    {
        Gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        Prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        Normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
    }

    /// <summary>LLM geçidi.</summary>
    protected IProviderGateway Gateway { get; }

    /// <summary>Prompt fabrikası.</summary>
    protected IConceptPromptFactory Prompts { get; }

    /// <summary>Konu hafızası.</summary>
    protected ITopicMemory Memory { get; }

    /// <summary>Normalleştirici.</summary>
    protected ITopicNormalizer Normalizer { get; }

    /// <summary>Başlık üretir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="count">Adet.</param>
    /// <param name="hint">Yön.</param>
    /// <param name="idea">Fikir.</param>
    /// <param name="memory">Hafıza.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öneriler.</returns>
    public virtual async Task<Result<TopicBatch>> GenerateAsync(ConceptGenerationContext context, int count, string? hint, string? idea, TopicMemorySnapshot memory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(memory);
        count = Math.Clamp(count, ConceptLimits.MinCount, ConceptLimits.MaxCount);
        var ask = Math.Min(count + Math.Max(2, count / 5), ConceptLimits.MaxCount + 20);
        var prompts = Prompts.Topics(context, Memory.RenderForPrompt(memory), hint?.Trim(), idea?.Trim(), ask);
        var task = new TextTask
        {
            UserId = context.UserId,
            ModelKey = context.Preferences.TopicModelKey,
            SystemPrompt = prompts.System,
            UserPrompt = prompts.User,
            MaxOutputTokens = Math.Clamp(400 + (ask * 60), 1000, 12000),
            Purpose = "topics",
            CacheSystemPrompt = true,
        };

        var result = await Gateway.RunStructuredAsync<TopicListLlmOutput>(task, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var accepted = new List<TopicSuggestionDto>(count);
        var seen = new List<TopicFingerprint>(count);
        var filtered = 0;
        foreach (var item in result.Value.Value.Topics ?? [])
        {
            var title = CleanTitle(item?.Title);
            if (title.Length == 0)
            {
                continue;
            }

            var fp = Normalizer.Fingerprint(title);
            if (fp.Words.Count == 0)
            {
                continue;
            }

            if (Memory.IsKnown(memory, fp) || seen.Any(s => Normalizer.IsDuplicate(s, fp)))
            {
                filtered++;
                continue;
            }

            seen.Add(fp);
            accepted.Add(new TopicSuggestionDto { Title = title, Hook = (item?.Hook ?? string.Empty).Trim() });
            if (accepted.Count == count)
            {
                break;
            }
        }

        if (accepted.Count == 0)
        {
            return ConceptErrors.NoTopics;
        }

        return new TopicBatch(accepted, filtered, result.Value.CostUsd);
    }

    /// <summary>Başlıktaki numara, madde işareti ve tırnakları temizler.</summary>
    /// <param name="title">Başlık.</param>
    /// <returns>Temiz başlık.</returns>
    protected static string CleanTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var t = LeadingMarkerRegex().Replace(title.Trim(), string.Empty);
        t = t.Trim().Trim('"', '“', '”', '\'', '«', '»').Trim();
        return t.Length <= ConceptLimits.TitleMaxLength ? t : t[..ConceptLimits.TitleMaxLength];
    }

    [GeneratedRegex(@"^\s*(\d+[\.\)\-:]\s*|[-•*–]\s*)")]
    private static partial Regex LeadingMarkerRegex();
}
