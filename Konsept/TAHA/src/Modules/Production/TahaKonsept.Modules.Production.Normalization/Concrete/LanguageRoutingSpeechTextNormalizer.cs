using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Concrete;

/// <summary>
/// DI'da <see cref="ISpeechTextNormalizer"/> olarak kayıtlı giriş noktası: dil koduna göre <see cref="INormalizerSelector"/>
/// ile uygun normalleştiriciyi seçip işi ona devreder (tr → tam Türkçe zincir, diğerleri → yalnız sözlük).
/// </summary>
public sealed class LanguageRoutingSpeechTextNormalizer : SpeechTextNormalizerBase
{
    private readonly INormalizerSelector _selector;

    /// <summary>Yönlendiriciyi kurar.</summary>
    /// <param name="selector">Dil seçici.</param>
    public LanguageRoutingSpeechTextNormalizer(INormalizerSelector selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    /// <inheritdoc />
    protected override NormalizedText NormalizeCore(string text, string language, IReadOnlyList<PronunciationEntry> lexicon) =>
        _selector.Select(language).Normalize(text, language, lexicon);
}
