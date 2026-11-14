using System.Text.RegularExpressions;

namespace TahaKonsept.Modules.Production.Normalization.Text;

/// <summary>Romen rakamı doğrulama ve çözümleme (I … MMMCMXCIX). Geçersiz biçimler (IIII, VX, IC) reddedilir.
/// Önemli üye: <see cref="TryParse"/>.</summary>
public static partial class RomanNumerals
{
    /// <summary>Geçerli bir Romen rakamını sayıya çevirir.</summary>
    /// <param name="value">Büyük harfli Romen rakamı.</param>
    /// <param name="number">Sonuç (1..3999).</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    public static bool TryParse(string value, out int number)
    {
        number = 0;
        if (string.IsNullOrEmpty(value) || !Strict().IsMatch(value))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            var current = ValueOf(value[i]);
            var next = i + 1 < value.Length ? ValueOf(value[i + 1]) : 0;
            number += current < next ? -current : current;
        }

        return number > 0;
    }

    private static int ValueOf(char c) => c switch
    {
        'I' => 1,
        'V' => 5,
        'X' => 10,
        'L' => 50,
        'C' => 100,
        'D' => 500,
        'M' => 1000,
        _ => 0,
    };

    [GeneratedRegex("^M{0,3}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$", RegexOptions.CultureInvariant)]
    private static partial Regex Strict();
}
