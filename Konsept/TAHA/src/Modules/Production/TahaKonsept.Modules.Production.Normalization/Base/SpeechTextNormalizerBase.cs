using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Base;

/// <summary>
/// Tüm seslendirme normalleştiricilerinin tabanı (şablon yöntem): girdi doğrulama, boş metin kısa devresi ve
/// <see cref="NormalizeCore"/> çağrısı. Boş dil kodu Türkçe (<see cref="DefaultLanguage"/>) kabul edilir.
/// Yazılış (<see cref="NormalizedText.Display"/>) daima girdinin aynısıdır.
/// </summary>
public abstract class SpeechTextNormalizerBase : ISpeechTextNormalizer
{
    /// <summary>Dil verilmezse kullanılan dil.</summary>
    public const string DefaultLanguage = "tr";

    /// <inheritdoc />
    public NormalizedText Normalize(string text, string language, IReadOnlyList<PronunciationEntry> lexicon)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new NormalizedText(text, text, []);
        }

        var lang = string.IsNullOrWhiteSpace(language) ? DefaultLanguage : language.Trim();
        return NormalizeCore(text, lang, lexicon ?? []);
    }

    /// <summary>Boş olmayan metni normalize eder.</summary>
    /// <param name="text">Yazılış.</param>
    /// <param name="language">Dil kodu (boş değil).</param>
    /// <param name="lexicon">Kullanıcı/konsept sözlüğü.</param>
    /// <returns>Sonuç.</returns>
    protected abstract NormalizedText NormalizeCore(string text, string language, IReadOnlyList<PronunciationEntry> lexicon);
}
