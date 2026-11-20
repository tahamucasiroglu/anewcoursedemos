namespace OkulYonetim.Domain.Identity;

/// <summary>
/// Türkiye telefon numarası (spec 0005, Ö-3): "0 5xx xxx xx xx", "5xx…", "+90 5xx…", "0212 …" gibi yazımlar kabul edilir;
/// boşluk, tire, parantez ve nokta yok sayılır. Ulusal numara 10 hanedir ve 2–5 ile başlar (sabit hat veya cep). Tek
/// biçimde (<c>+90XXXXXXXXXX</c>) saklanır.
/// </summary>
public static class PhoneNumber
{
    /// <summary>Boş ise <c>null</c> (telefon isteğe bağlı) ve geçerli; geçersizse <c>false</c>.</summary>
    public static bool TryNormalize(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        string trimmed = value.Trim();
        bool international = trimmed.StartsWith('+');
        string digits = new([.. trimmed.Where(char.IsAsciiDigit)]);
        if (trimmed.Any(c => !char.IsAsciiDigit(c) && c is not (' ' or '-' or '(' or ')' or '.' or '+')))
        {
            return false;
        }

        string national = international
            ? (digits.StartsWith("90", StringComparison.Ordinal) ? digits[2..] : string.Empty)
            : digits.StartsWith("90", StringComparison.Ordinal) && digits.Length == 12 ? digits[2..]
            : digits.StartsWith('0') ? digits[1..]
            : digits;
        if (national.Length != 10 || national[0] is < '2' or > '5')
        {
            return false;
        }

        normalized = "+90" + national;
        return true;
    }
}
