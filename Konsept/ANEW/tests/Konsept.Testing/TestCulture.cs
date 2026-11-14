using System.Globalization;

namespace Konsept.Testing;

/// <summary>
/// Testlerin kültürünü <c>tr-TR</c>'ye sabitler (docs/testing.md, Belirlenimcilik): Türkçe I/İ
/// tuzağını testler yakalasın. Her test projesi bunu bir modül başlatıcısından çağırır.
/// </summary>
public static class TestCulture
{
    /// <summary>Gets testlerde kullanılan kültür.</summary>
    public static CultureInfo Turkish { get; } = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Süreç genelinde varsayılan kültürü ve arayüz kültürünü <c>tr-TR</c> yapar.</summary>
    public static void Apply()
    {
        CultureInfo.DefaultThreadCurrentCulture = Turkish;
        CultureInfo.DefaultThreadCurrentUICulture = Turkish;
        CultureInfo.CurrentCulture = Turkish;
        CultureInfo.CurrentUICulture = Turkish;
    }
}
