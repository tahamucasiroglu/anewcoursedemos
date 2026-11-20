using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Security;

/// <summary>
/// Özel nitelikli kişisel veri (spec 0009: sağlık notu, alerji) için alan düzeyinde AES-GCM şifreleme. Anahtar iki adımlı
/// doğrulamanınkinden <b>ayrıdır</b>: <c>PersonalData:EncryptionKey</c> (Base64, 32 bayt) ortam ayarından — repoda yok; Pi'de
/// <c>/etc/okul/okul-data.env</c> (root 600). Biri sızarsa öteki korunur. Anahtar yoksa ya da bozuksa
/// <see cref="IsConfigured"/> false ve hata logu; öğrenci kartı çalışır, sağlık alanları kapalı kalır. Anahtar ve düz metin
/// loglanmaz.
/// </summary>
public sealed class AesGcmPersonalDataProtector : IPersonalDataProtector
{
    public const string SettingName = "PersonalData:EncryptionKey";

    private readonly byte[]? key;

    public AesGcmPersonalDataProtector(IConfiguration configuration, ILogger<AesGcmPersonalDataProtector> logger)
    {
        this.key = AesGcmCipher.ReadKey(
            configuration,
            SettingName,
            logger,
            "sağlık bilgisi şifrelemesi");
    }

    public bool IsConfigured => this.key is not null;

    public string Protect(string plaintext) => AesGcmCipher.Encrypt(this.Key(), plaintext);

    public string Unprotect(string cipher) => AesGcmCipher.Decrypt(this.Key(), cipher);

    private byte[] Key() => this.key ?? throw new InvalidOperationException("Kişisel veri şifreleme anahtarı yapılandırılmamış.");
}
