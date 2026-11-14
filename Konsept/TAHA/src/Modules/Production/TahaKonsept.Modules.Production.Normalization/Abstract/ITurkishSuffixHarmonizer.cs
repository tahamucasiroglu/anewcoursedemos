namespace TahaKonsept.Modules.Production.Normalization.Abstract;

/// <summary>
/// Kesme işaretiyle ayrılmış eki okunuş gövdesine ses uyumuyla ekler: ünlü uyumu (a/e, ı/i/u/ü), ünsüz benzeşmesi (d/t, c/ç),
/// kaynaştırma ünsüzü (y/n) ve sayı yumuşaması (dört + e → dörde). Örnek: ("bin dört yüz elli üç", "de") → "…üçte".
/// Önemli üyeler: <see cref="HarmonizeSuffix"/>, <see cref="Attach"/>.
/// </summary>
public interface ITurkishSuffixHarmonizer
{
    /// <summary>Eki gövdeye göre uyumlu hâle getirir (gövde değişmez; dönen değer yalnız ek).</summary>
    /// <param name="stem">Okunuş gövdesi (ör. "Gugıl").</param>
    /// <param name="suffix">Yazılıştaki ek (ör. "ın").</param>
    /// <returns>Uyumlu ek.</returns>
    string HarmonizeSuffix(string stem, string suffix);

    /// <summary>Eki gövdeye kesme işaretsiz bitiştirir; gerekiyorsa gövde yumuşar (dört → dörd).</summary>
    /// <param name="stem">Okunuş gövdesi.</param>
    /// <param name="suffix">Yazılıştaki ek (boş olabilir).</param>
    /// <returns>Gövde + uyumlu ek.</returns>
    string Attach(string stem, string suffix);
}
