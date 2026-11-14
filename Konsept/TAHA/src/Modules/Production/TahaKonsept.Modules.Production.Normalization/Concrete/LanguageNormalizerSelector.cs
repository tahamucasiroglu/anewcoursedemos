using TahaKonsept.Modules.Production.Normalization.Base;

namespace TahaKonsept.Modules.Production.Normalization.Concrete;

/// <summary>Varsayılan dil seçici: Türkçe → <see cref="TurkishSpeechTextNormalizer"/>, diğerleri →
/// <see cref="LexiconOnlySpeechTextNormalizer"/>.</summary>
public sealed class LanguageNormalizerSelector : NormalizerSelectorBase
{
    /// <summary>Seçiciyi kurar.</summary>
    /// <param name="turkish">Türkçe normalleştirici.</param>
    /// <param name="lexiconOnly">Yedek (yalnız sözlük) normalleştirici.</param>
    public LanguageNormalizerSelector(TurkishSpeechTextNormalizer turkish, LexiconOnlySpeechTextNormalizer lexiconOnly)
        : base([turkish], lexiconOnly)
    {
    }
}
