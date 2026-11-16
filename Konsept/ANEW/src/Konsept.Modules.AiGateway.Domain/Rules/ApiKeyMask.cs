namespace Konsept.Modules.AiGateway.Domain.Rules;

/// <summary>
/// Ayarlar'da gösterilen maskeli key (spec 0004 Req 9): ilk en fazla 3 + <c>…</c> + son 3 karakter (<c>sk-…a3f</c>). Kısa key'de
/// baştan daha az karakter gösterilir (baştan karakter ancak 6 karakter gizli kalabiliyorsa); 8 karakterlik key <c>…xyz</c> olur.
/// </summary>
public static class ApiKeyMask
{
    private const string Ellipsis = "…";

    private const int Tail = 3;

    private const int MaxHead = 3;

    private const int MinHidden = 6;

    /// <summary>Kırpılmış key'i maskeler.</summary>
    /// <param name="key">Geçerli (kırpılmış) key.</param>
    /// <returns>Maskeli hal.</returns>
    public static string Mask(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length < ApiKeyFormat.MinLength)
        {
            return Ellipsis;
        }

        var head = Math.Clamp(key.Length - Tail - MinHidden, 0, MaxHead);
        return string.Concat(key.AsSpan(0, head), Ellipsis, key.AsSpan(key.Length - Tail));
    }
}
