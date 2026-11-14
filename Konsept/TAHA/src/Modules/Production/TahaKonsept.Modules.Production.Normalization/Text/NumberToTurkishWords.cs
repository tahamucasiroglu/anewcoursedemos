using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Concrete;

namespace TahaKonsept.Modules.Production.Normalization.Text;

/// <summary>
/// Sayı → Türkçe açık yazı için statik kısayol (başka modüller/ajan DI'sız kullanabilir). Arkada tek bir
/// <see cref="TurkishNumberToWords"/> örneği vardır (iş parçacığı güvenli, durumsuz).
/// Örnek: <c>NumberToTurkishWords.ToWords(1453)</c> → "bin dört yüz elli üç"; <c>ToOrdinal(21)</c> → "yirmi birinci".
/// </summary>
public static class NumberToTurkishWords
{
    /// <summary>Paylaşılan örnek.</summary>
    public static INumberToTurkishWords Default { get; } = new TurkishNumberToWords();

    /// <summary>Tam sayıyı açık yazıya çevirir.</summary>
    /// <param name="value">Sayı.</param>
    /// <returns>Açık yazı.</returns>
    public static string ToWords(long value) => Default.ToWords(value);

    /// <summary>Rakam dizisini açık yazıya çevirir (baştaki sıfırlar "sıfır").</summary>
    /// <param name="digits">Rakamlar.</param>
    /// <returns>Açık yazı.</returns>
    public static string ToWords(string digits) => Default.ToWords(digits);

    /// <summary>Ondalık sayıyı "virgül" ile okur.</summary>
    /// <param name="integerDigits">Tam kısım.</param>
    /// <param name="fractionDigits">Kesir kısmı.</param>
    /// <returns>Açık yazı.</returns>
    public static string ToDecimalWords(string integerDigits, string fractionDigits) =>
        Default.ToDecimalWords(integerDigits, fractionDigits, "virgül");

    /// <summary>Sıra sayısı (3 → üçüncü).</summary>
    /// <param name="value">Sayı.</param>
    /// <returns>Açık yazı.</returns>
    public static string ToOrdinal(long value) => Default.ToOrdinal(value);
}
