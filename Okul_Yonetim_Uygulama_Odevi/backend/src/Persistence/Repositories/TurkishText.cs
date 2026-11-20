using System.Globalization;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Türkçe harf kuralıyla (ı/I, i/İ, ş/Ş, ö/Ö…) büyük/küçük harf duyarsız karşılaştırma ve arama.</summary>
public static class TurkishText
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static StringComparer IgnoreCase { get; } = StringComparer.Create(Turkish, ignoreCase: true);

    public static bool Contains(string value, string search)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(search);
        return Turkish.CompareInfo.IndexOf(value, search.Trim(), CompareOptions.IgnoreCase) >= 0;
    }
}
