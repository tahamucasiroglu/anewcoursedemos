namespace TahaKonsept.Modules.Production.Normalization.Abstract;

/// <summary>
/// Sayıları Türkçe açık yazıya çevirir: 1453 → "bin dört yüz elli üç", -5 → "eksi beş", 3,5 → "üç virgül beş",
/// 21 → "yirmi birinci" (sıra). Trilyon ve üstü (katrilyon, kentilyon) desteklenir.
/// Önemli üyeler: <see cref="ToWords(long)"/>, <see cref="ToWords(string)"/>, <see cref="ToDecimalWords"/>,
/// <see cref="ToOrdinal"/>, <see cref="ToOrdinalWords"/>, <see cref="Vocabulary"/>.
/// </summary>
public interface INumberToTurkishWords
{
    /// <summary>Tam sayıyı açık yazıya çevirir (negatifse "eksi" ile başlar).</summary>
    /// <param name="value">Sayı.</param>
    /// <returns>Açık yazı (ör. "bin dokuz yüz yirmi üç").</returns>
    string ToWords(long value);

    /// <summary>Rakam dizisini açık yazıya çevirir. Baştaki sıfırlar tek tek "sıfır" okunur (007 → "sıfır sıfır yedi"),
    /// 18 haneden uzun diziler rakam rakam okunur.</summary>
    /// <param name="digits">Yalnız rakamlardan oluşan dizi.</param>
    /// <returns>Açık yazı.</returns>
    string ToWords(string digits);

    /// <summary>Ondalık sayıyı okur: tam kısım + ayırıcı sözcük + kesir kısmı (3,05 → "üç virgül sıfır beş").</summary>
    /// <param name="integerDigits">Tam kısmın rakamları.</param>
    /// <param name="fractionDigits">Kesir kısmının rakamları.</param>
    /// <param name="separatorWord">Ayırıcı sözcük ("virgül" ya da "nokta").</param>
    /// <returns>Açık yazı.</returns>
    string ToDecimalWords(string integerDigits, string fractionDigits, string separatorWord);

    /// <summary>Sıra sayısını açık yazıya çevirir (3 → "üçüncü", 21 → "yirmi birinci").</summary>
    /// <param name="value">Sayı.</param>
    /// <returns>Sıra sayısı açık yazısı.</returns>
    string ToOrdinal(long value);

    /// <summary>Açık yazılmış asıl sayıyı sıra sayısına çevirir ("yirmi dört" → "yirmi dördüncü").</summary>
    /// <param name="cardinalWords">Asıl sayı açık yazısı.</param>
    /// <returns>Sıra sayısı açık yazısı.</returns>
    string ToOrdinalWords(string cardinalWords);

    /// <summary>Sayı sözcükleri (sıfır, bir … trilyon); başka kurallar "sayı bağlamı" tespiti için kullanır.</summary>
    IReadOnlyCollection<string> Vocabulary { get; }
}
