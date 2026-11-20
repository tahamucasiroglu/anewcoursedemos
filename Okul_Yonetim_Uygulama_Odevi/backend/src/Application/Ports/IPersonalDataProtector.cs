namespace OkulYonetim.Application.Ports;

/// <summary>
/// Özel nitelikli kişisel veriyi (spec 0009: sağlık notu, alerji) veritabanında şifreli tutmak için. Anahtar ortam
/// ayarından gelir ve iki adımlı doğrulama anahtarından ayrıdır; ayarlı değilse <see cref="IsConfigured"/> false — bu durumda
/// çağıran sağlık alanlarını "gösterilemiyor / kaydedilemiyor" sayar.
/// </summary>
public interface IPersonalDataProtector
{
    bool IsConfigured { get; }

    string Protect(string plaintext);

    string Unprotect(string cipher);
}
