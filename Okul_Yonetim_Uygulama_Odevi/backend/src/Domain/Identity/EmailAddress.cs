namespace OkulYonetim.Domain.Identity;

/// <summary>
/// E-posta adresinin karşılaştırma biçimi: baştaki/sondaki boşluk atılır, küçük harfe çevrilir (kültürden bağımsız —
/// T1). Kişi bu biçimle tekildir; giriş de bu biçimle eşleşir (spec 0004, gereksinim 3).
/// </summary>
public static class EmailAddress
{
    public static string Normalize(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
#pragma warning disable CA1308 // E-posta teknik değer; küçük harf yaygın biçimdir ve kültürden bağımsız çevrilir.
        return email.Trim().ToLowerInvariant();
#pragma warning restore CA1308
    }
}
