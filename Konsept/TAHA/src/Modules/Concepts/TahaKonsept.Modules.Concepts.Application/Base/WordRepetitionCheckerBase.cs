using System.Globalization;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Common;
using TahaKonsept.Modules.Concepts.Application.Llm;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IWordRepetitionChecker"/> ortak davranışı: yerel analiz → bulgu yoksa ücretsiz biter; varsa yalnız ilgili sahneleri
/// "[index] metin" satırlarıyla ucuz modele gönderir ve dönen metni, kelime sayısı orijinalin %70–%130'u arasındaysa uygular.
/// </summary>
public abstract class WordRepetitionCheckerBase : IWordRepetitionChecker
{
    /// <summary>Oluşturur.</summary>
    /// <param name="analyzer">Yerel analizci.</param>
    /// <param name="gateway">LLM geçidi.</param>
    /// <param name="prompts">Prompt fabrikası.</param>
    protected WordRepetitionCheckerBase(IWordRepetitionAnalyzer analyzer, IProviderGateway gateway, IConceptPromptFactory prompts)
    {
        Analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        Gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        Prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
    }

    /// <summary>Yerel analizci.</summary>
    protected IWordRepetitionAnalyzer Analyzer { get; }

    /// <summary>LLM geçidi.</summary>
    protected IProviderGateway Gateway { get; }

    /// <summary>Prompt fabrikası.</summary>
    protected IConceptPromptFactory Prompts { get; }

    /// <summary>Kabul edilen en düşük uzunluk oranı.</summary>
    protected virtual double MinLengthRatio => 0.7;

    /// <summary>Kabul edilen en yüksek uzunluk oranı.</summary>
    protected virtual double MaxLengthRatio => 1.3;

    /// <summary>Belgeyi denetler.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="document">Belge.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public virtual async Task<Result<WordCheckOutcome>> CheckAsync(ConceptGenerationContext context, ConceptDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);
        var findings = Analyzer.Analyze(document);
        if (findings.Count == 0)
        {
            return new WordCheckOutcome(document, findings, 0, 0, 0, 0);
        }

        var affected = findings.SelectMany(f => f.SceneIndexes).Where(i => i >= 0 && i < document.Scenes.Count).Distinct().Order().ToList();
        var block = string.Join('\n', affected.Select(i => string.Create(CultureInfo.InvariantCulture, $"[{i}] {document.Scenes[i].Narration.Trim()}")));
        var words = affected.Sum(i => TurkishText.CountWords(document.Scenes[i].Narration));
        var prompts = Prompts.WordCheck(findings.Select(f => f.Word).ToList(), block);
        var task = new TextTask
        {
            UserId = context.UserId,
            ModelKey = context.Preferences.WordCheckModelKey,
            SystemPrompt = prompts.System,
            UserPrompt = prompts.User,
            MaxOutputTokens = Math.Clamp((words * 5) + 300, 600, 8000),
            Purpose = "word-check",
            CacheSystemPrompt = true,
        };

        var result = await Gateway.RunStructuredAsync<NarrationRewriteLlmOutput>(task, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var scenes = document.Scenes.ToList();
        var allowed = affected.ToHashSet();
        var rewritten = 0;
        foreach (var item in result.Value.Value.Scenes ?? [])
        {
            if (item is null || !allowed.Remove(item.Index))
            {
                continue;
            }

            var text = (item.Narration ?? string.Empty).Trim();
            var original = TurkishText.CountWords(scenes[item.Index].Narration);
            var ratio = (double)TurkishText.CountWords(text) / Math.Max(1, original);
            if (text.Length == 0 || ratio < MinLengthRatio || ratio > MaxLengthRatio)
            {
                continue;
            }

            scenes[item.Index] = scenes[item.Index] with { Narration = text };
            rewritten++;
        }

        var usage = result.Value.Usage;
        return new WordCheckOutcome(document with { Scenes = scenes }, findings, rewritten, usage.TotalInputTokens, usage.OutputTokens, result.Value.CostUsd);
    }
}
