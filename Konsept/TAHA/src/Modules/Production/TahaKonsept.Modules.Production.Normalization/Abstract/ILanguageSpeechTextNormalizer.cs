namespace TahaKonsept.Modules.Production.Normalization.Abstract;

/// <summary>
/// Belirli dil(ler)e özgü normalleştirici: <see cref="ISpeechTextNormalizer"/> + hangi dili desteklediği bilgisi.
/// <see cref="INormalizerSelector"/> dil koduna göre bu arayüzü uygulayanlar arasından seçim yapar.
/// Önemli üyeler: <see cref="Name"/>, <see cref="Supports"/>.
/// </summary>
public interface ILanguageSpeechTextNormalizer : ISpeechTextNormalizer
{
    /// <summary>Normalleştiricinin kısa adı (ör. <c>turkish</c>, <c>lexicon-only</c>).</summary>
    string Name { get; }

    /// <summary>Verilen dil kodunu (ör. <c>tr</c>, <c>tr-TR</c>, <c>en</c>) destekliyor mu.</summary>
    /// <param name="language">Dil kodu.</param>
    /// <returns>Destekliyorsa <c>true</c>.</returns>
    bool Supports(string language);
}
