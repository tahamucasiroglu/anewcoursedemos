using System.Globalization;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Application.Prompts;
using TahaKonsept.Modules.Concepts.Domain.ValueObjects;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptPromptFactory"/> ortak davranışı. Konsept sistem promptu kanal metni + ince ayar + süre/kelime/sahne hedefleri +
/// kadraj + gerçek bilgi bayrağıyla işlenir (bir iş boyunca sabit → önbellek). Gerçek bilgi açık + model web araması yapabiliyorsa
/// <c>fact</c> (doğrula, en az 2 gerçek kaynak); yapamıyorsa <c>factOffline</c> (yalnız yerleşik bilgi, kaynak boş kalabilir, şüpheliler
/// <c>reviewNotes</c>'a) bölümü açılır. Düzeltme aynı sistem promptunu kullanır.
/// </summary>
public abstract class ConceptPromptFactoryBase : IConceptPromptFactory
{
    private static readonly Dictionary<string, string> LanguageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tr"] = "Türkçe",
        ["en"] = "İngilizce",
        ["de"] = "Almanca",
        ["fr"] = "Fransızca",
        ["es"] = "İspanyolca",
        ["it"] = "İtalyanca",
        ["ru"] = "Rusça",
        ["ar"] = "Arapça",
        ["az"] = "Azerbaycan Türkçesi",
    };

    /// <summary>Oluşturur.</summary>
    /// <param name="library">Şablon kütüphanesi.</param>
    /// <param name="renderer">Şablon işleyici.</param>
    protected ConceptPromptFactoryBase(IPromptLibrary library, IPromptTemplateRenderer renderer)
    {
        Library = library ?? throw new ArgumentNullException(nameof(library));
        Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>Şablon kütüphanesi.</summary>
    protected IPromptLibrary Library { get; }

    /// <summary>Şablon işleyici.</summary>
    protected IPromptTemplateRenderer Renderer { get; }

    /// <summary>Konu önerisi promptu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="memoryBlock">Hafıza listesi.</param>
    /// <param name="hint">Yön.</param>
    /// <param name="idea">Fikir.</param>
    /// <param name="count">Adet.</param>
    /// <returns>Prompt çifti.</returns>
    public virtual PromptPair Topics(ConceptGenerationContext context, string memoryBlock, string? hint, string? idea, int count)
    {
        ArgumentNullException.ThrowIfNull(context);
        var style = StyleLine(context.Preferences);
        var profile = style.Length == 0 ? context.ProfileBlock : context.ProfileBlock + "\nİnce ayar: " + style;
        var user = Render(PromptNames.TopicsUser, new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["profile"] = profile,
            ["memory"] = memoryBlock,
            ["hint"] = hint,
            ["idea"] = idea,
            ["count"] = count.ToString(CultureInfo.InvariantCulture),
        });
        return new PromptPair(Render(PromptNames.TopicsSystem, Empty), user);
    }

    /// <summary>Konsept üretim promptu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="brief">Girdi.</param>
    /// <returns>Prompt çifti.</returns>
    public virtual PromptPair Concept(ConceptGenerationContext context, ConceptBrief brief)
    {
        ArgumentNullException.ThrowIfNull(brief);
        var user = Render(PromptNames.ConceptUser, new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["title"] = brief.Title,
            ["hook"] = brief.Hook,
            ["idea"] = brief.Idea,
            ["angle"] = brief.AngleNumber?.ToString(CultureInfo.InvariantCulture),
        });
        return new PromptPair(ConceptSystem(context), user);
    }

    /// <summary>Düzeltme promptu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="documentJson">Mevcut belge JSON'u.</param>
    /// <param name="instruction">Talimat.</param>
    /// <returns>Prompt çifti.</returns>
    public virtual PromptPair Revise(ConceptGenerationContext context, string documentJson, string instruction)
    {
        var user = Render(PromptNames.ReviseUser, new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["document"] = documentJson,
            ["instruction"] = instruction,
        });
        return new PromptPair(ConceptSystem(context), user);
    }

    /// <summary>Kelime denetimi promptu.</summary>
    /// <param name="words">Fazla tekrar edenler.</param>
    /// <param name="scenesBlock">Sahne satırları.</param>
    /// <returns>Prompt çifti.</returns>
    public virtual PromptPair WordCheck(IReadOnlyList<string> words, string scenesBlock)
    {
        ArgumentNullException.ThrowIfNull(words);
        var user = Render(PromptNames.WordCheckUser, new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["words"] = string.Join(", ", words),
            ["scenes"] = scenesBlock,
        });
        return new PromptPair(Render(PromptNames.WordCheckSystem, Empty), user);
    }

    /// <summary>Konsept sistem promptunu işler.</summary>
    /// <param name="context">Bağlam.</param>
    /// <returns>Sistem promptu.</returns>
    protected virtual string ConceptSystem(ConceptGenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var inv = CultureInfo.InvariantCulture;
        return Render(PromptNames.ConceptSystem, new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["profile"] = context.ProfileBlock,
            ["style"] = StyleLine(context.Preferences),
            ["notes"] = context.Preferences.Notes,
            ["language"] = LanguageName(context.Language),
            ["duration"] = context.TargetDurationSec.ToString(inv),
            ["words"] = context.TargetWords.ToString(inv),
            ["wps"] = context.WordsPerSecond.ToString("0.0", inv),
            ["scenes"] = context.SceneCountHint.ToString(inv),
            ["aspect"] = context.Format == VideoFormat.Horizontal ? "16:9 yatay (horizontal 16:9)" : "9:16 dikey (vertical 9:16)",
            ["fact"] = context.FactGrounding && context.WebSearchAvailable ? "1" : null,
            ["factOffline"] = context.FactGrounding && !context.WebSearchAvailable ? "1" : null,
        });
    }

    /// <summary>İnce ayar satırı (yalnız kullanıcı ayarında dolu olanlar; profil değerleri zaten kanal metninde).</summary>
    /// <param name="preferences">Tercihler.</param>
    /// <returns>Satır ya da boş.</returns>
    protected static string StyleLine(ConceptPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(preferences.Tone))
        {
            parts.Add("Ton: " + preferences.Tone.Trim());
        }

        if (!string.IsNullOrWhiteSpace(preferences.Audience))
        {
            parts.Add("İzleyici: " + preferences.Audience.Trim());
        }

        if (!string.IsNullOrWhiteSpace(preferences.NarrativeStyle))
        {
            parts.Add("Anlatım: " + preferences.NarrativeStyle.Trim());
        }

        return string.Join(" | ", parts);
    }

    /// <summary>Dil kodunun Türkçe adı (bilinmiyorsa kod).</summary>
    /// <param name="language">Dil kodu.</param>
    /// <returns>Ad.</returns>
    protected static string LanguageName(string? language)
    {
        var code = string.IsNullOrWhiteSpace(language) ? "tr" : language.Trim();
        var primary = code.Split('-')[0];
        return LanguageNames.TryGetValue(primary, out var name) ? name : code;
    }

    private static readonly IReadOnlyDictionary<string, string?> Empty = new Dictionary<string, string?>(StringComparer.Ordinal);

    private string Render(string name, IReadOnlyDictionary<string, string?> values) => Renderer.Render(Library.Get(name), values);
}
