using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Concrete.Rules;

namespace TahaKonsept.Modules.Production.Normalization.Concrete;

/// <summary>
/// Türkçe dışı diller için yedek normalleştirici: yalnız kullanıcı/konsept sözlüğü uygulanır (Türkçe yerleşik sözlük ve
/// sayı/tarih kuralları UYGULANMAZ; kesme ekleri aynen kalır). Sözlük boşsa metin olduğu gibi geçer. Her dili destekler.
/// </summary>
public sealed class LexiconOnlySpeechTextNormalizer : RuleChainSpeechTextNormalizerBase
{
    /// <summary>Zinciri kurar.</summary>
    public LexiconOnlySpeechTextNormalizer()
        : base([new LexiconRule([], harmonizer: null)])
    {
    }

    /// <inheritdoc />
    public override string Name => "lexicon-only";

    /// <inheritdoc />
    public override bool Supports(string language) => true;
}
