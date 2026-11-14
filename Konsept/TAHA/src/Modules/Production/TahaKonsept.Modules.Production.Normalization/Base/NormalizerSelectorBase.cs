using TahaKonsept.Modules.Production.Normalization.Abstract;

namespace TahaKonsept.Modules.Production.Normalization.Base;

/// <summary>
/// Dil seçicinin tabanı: kayıtlı dil normalleştiricileri sırayla denenir, dili destekleyen ilki döner; hiçbiri desteklemezse
/// <see cref="Fallback"/> (yalnız sözlük) döner. Boş dil kodu Türkçe sayılır.
/// </summary>
public abstract class NormalizerSelectorBase : INormalizerSelector
{
    private readonly IReadOnlyList<ILanguageSpeechTextNormalizer> _normalizers;

    /// <summary>Seçiciyi kurar.</summary>
    /// <param name="normalizers">Dile özgü normalleştiriciler (öncelik sırasıyla).</param>
    /// <param name="fallback">Yedek normalleştirici.</param>
    protected NormalizerSelectorBase(IEnumerable<ILanguageSpeechTextNormalizer> normalizers, ILanguageSpeechTextNormalizer fallback)
    {
        ArgumentNullException.ThrowIfNull(normalizers);
        Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _normalizers = normalizers.Where(n => !ReferenceEquals(n, fallback)).ToList();
    }

    /// <summary>Yedek normalleştirici.</summary>
    protected ILanguageSpeechTextNormalizer Fallback { get; }

    /// <inheritdoc />
    public ILanguageSpeechTextNormalizer Select(string language)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? SpeechTextNormalizerBase.DefaultLanguage : language.Trim();
        return _normalizers.FirstOrDefault(n => n.Supports(lang)) ?? Fallback;
    }
}
