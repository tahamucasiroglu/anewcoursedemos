using System.Globalization;

namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>
/// Dile göre küçük harfe çevirme (spec 0014 Req 14, S-2): Türkçe metinde I/İ kuralı (<c>Işık</c> → <c>ışık</c>, <c>İz</c> → <c>iz</c>),
/// diğer dillerde kültürden bağımsız. Çalışan iş parçacığının kültürüne bağlı değildir.
/// </summary>
public static class TextCase
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Küçük harf.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="language">Dil kodu (<c>tr</c> · <c>en</c>).</param>
    /// <returns>Küçük harfli metin.</returns>
    public static string Lower(string text, string language) =>
        string.Equals(language, "tr", StringComparison.Ordinal) ? Turkish.TextInfo.ToLower(text) : text.ToLowerInvariant();
}
