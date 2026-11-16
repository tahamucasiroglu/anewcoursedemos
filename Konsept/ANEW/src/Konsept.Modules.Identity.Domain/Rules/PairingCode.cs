namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// Ajan eşleştirme kodu kuralı (spec 0012 Req 1): 8 karakter, <c>ABCD-1234</c> biçiminde (4 + tire + 4); karışan karakterler
/// (O/0, I/1) alfabede yoktur. Kullanıcının yazdığı kod kırpılır, büyük harfe çevrilir ve tiresiz yazılmışsa tire eklenir.
/// </summary>
public static class PairingCode
{
    /// <summary>Kodda kullanılan karakterler (O, 0, I, 1 hariç).</summary>
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Tiresiz karakter sayısı.</summary>
    public const int Length = 8;

    /// <summary>Yeni kod üretir.</summary>
    /// <param name="nextIndex">Kriptografik rastgele, <c>[0, üst)</c> aralığında tam sayı veren kaynak.</param>
    /// <returns><c>ABCD-1234</c> biçiminde kod.</returns>
    public static string Generate(Func<int, int> nextIndex)
    {
        ArgumentNullException.ThrowIfNull(nextIndex);
        var chars = new char[Length + 1];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = i == Length / 2 ? '-' : Alphabet[nextIndex(Alphabet.Length)];
        }

        return new string(chars);
    }

    /// <summary>Kullanıcının yazdığı kodu kanonik biçime çevirir.</summary>
    /// <param name="input">Girilen kod.</param>
    /// <returns>Kanonik kod; biçim tutmuyorsa <c>null</c>.</returns>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var compact = input.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
        if (compact.Length != Length || compact.Any(ch => !Alphabet.Contains(ch, StringComparison.Ordinal)))
        {
            return null;
        }

        return string.Concat(compact.AsSpan(0, Length / 2), "-", compact.AsSpan(Length / 2));
    }
}
