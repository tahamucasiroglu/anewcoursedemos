using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Factory;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Tests.Support;

/// <summary>Testler için paylaşılan, DI'sız kurulmuş normalleştirici ve kısayollar.</summary>
public static class TestNormalizer
{
    /// <summary>Varsayılan (dil yönlendirmeli) normalleştirici.</summary>
    public static ISpeechTextNormalizer Default { get; } = SpeechTextNormalizerFactory.CreateDefault();

    /// <summary>Türkçe normalizasyon sonucu.</summary>
    /// <param name="text">Yazılış.</param>
    /// <param name="lexicon">İsteğe bağlı kullanıcı sözlüğü.</param>
    /// <returns>Sonuç.</returns>
    public static NormalizedText Run(string text, params PronunciationEntry[] lexicon) => Default.Normalize(text, "tr", lexicon);

    /// <summary>Türkçe okunuş metni.</summary>
    /// <param name="text">Yazılış.</param>
    /// <returns>Okunuş.</returns>
    public static string Spoken(string text) => Run(text).Spoken;

    /// <summary>Sözlük girdisi kısayolu.</summary>
    /// <param name="written">Yazılış.</param>
    /// <param name="spoken">Okunuş.</param>
    /// <returns>Girdi.</returns>
    public static PronunciationEntry Entry(string written, string spoken) => new() { Written = written, Spoken = spoken };
}
