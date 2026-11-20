using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Security;

/// <summary>
/// AES-GCM ile gizli değer şifreleme (spec 0007): anahtar <c>TwoFactor:EncryptionKey</c> (Base64, 32 bayt) ortam
/// ayarından — repoda yok; Pi'de root'un okuyabildiği ayrı dosyada. Şifreleme ortak <see cref="AesGcmCipher"/> çekirdeğiyle.
/// Anahtar yoksa ya da biçimi bozuksa (Base64 değil / 32 bayt değil) <see cref="IsConfigured"/> false ve hata logu; platform
/// yöneticisi girişi güvenle reddedilir, diğer girişler etkilenmez (review round 1, L-1). Anahtar değeri loglanmaz.
/// </summary>
public sealed class AesGcmSecretProtector : ISecretProtector
{
    private readonly byte[]? key;

    public AesGcmSecretProtector(IConfiguration configuration, ILogger<AesGcmSecretProtector> logger)
    {
        this.key = AesGcmCipher.ReadKey(
            configuration,
            "TwoFactor:EncryptionKey",
            logger,
            "iki adımlı doğrulama");
    }

    public bool IsConfigured => this.key is not null;

    public string Protect(string plaintext) => AesGcmCipher.Encrypt(this.Key(), plaintext);

    public string Unprotect(string cipher) => AesGcmCipher.Decrypt(this.Key(), cipher);

    private byte[] Key() => this.key ?? throw new InvalidOperationException("İki adımlı doğrulama şifreleme anahtarı ayarlı değil.");
}
