using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Concrete;
using TahaKonsept.Modules.Production.Normalization.Text;

namespace TahaKonsept.Modules.Production.Normalization.Factory;

/// <summary>
/// DI'sız kurulum (testler, masaüstü ajanı, betikler): <see cref="CreateDefault"/> dil yönlendirmeli normalleştiriciyi,
/// <see cref="CreateTurkish"/> doğrudan Türkçe zinciri, <see cref="CreateSelector"/> dil seçiciyi döndürür.
/// Yerleşik sözlük gömülü kaynaktan bir kez okunur.
/// </summary>
public static class SpeechTextNormalizerFactory
{
    private static readonly Lazy<EmbeddedBuiltInLexiconProvider> Lexicon = new(() => new EmbeddedBuiltInLexiconProvider());

    /// <summary>Türkçe tam zincir.</summary>
    /// <returns>Normalleştirici.</returns>
    public static TurkishSpeechTextNormalizer CreateTurkish() =>
        new(Lexicon.Value, NumberToTurkishWords.Default, new TurkishSuffixHarmonizer());

    /// <summary>Dil seçici (Türkçe + yalnız sözlük yedeği).</summary>
    /// <returns>Seçici.</returns>
    public static INormalizerSelector CreateSelector() =>
        new LanguageNormalizerSelector(CreateTurkish(), new LexiconOnlySpeechTextNormalizer());

    /// <summary>Dil yönlendirmeli varsayılan normalleştirici.</summary>
    /// <returns>Normalleştirici.</returns>
    public static ISpeechTextNormalizer CreateDefault() => new LanguageRoutingSpeechTextNormalizer(CreateSelector());
}
