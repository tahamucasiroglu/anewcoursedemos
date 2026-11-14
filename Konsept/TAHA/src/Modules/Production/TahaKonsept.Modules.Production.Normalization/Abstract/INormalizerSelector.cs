namespace TahaKonsept.Modules.Production.Normalization.Abstract;

/// <summary>
/// Dil koduna göre uygun normalleştiriciyi seçer: Türkçe → tam zincir (<c>TurkishSpeechTextNormalizer</c>),
/// diğer diller → yalnız kullanıcı sözlüğü (<c>LexiconOnlySpeechTextNormalizer</c>). Önemli üye: <see cref="Select"/>.
/// </summary>
public interface INormalizerSelector
{
    /// <summary>Dil kodu için normalleştiriciyi döndürür (hiçbiri desteklemiyorsa yedek normalleştirici).</summary>
    /// <param name="language">Dil kodu (boşsa Türkçe kabul edilir).</param>
    /// <returns>Seçilen normalleştirici.</returns>
    ILanguageSpeechTextNormalizer Select(string language);
}
