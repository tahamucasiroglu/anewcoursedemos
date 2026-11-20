using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Security;

/// <summary>
/// AES-GCM ile gizli değer şifreleme (spec 0007): anahtar <c>TwoFactor:EncryptionKey</c> (Base64, 32 bayt) ortam
/// ayarından — repoda yok; Pi'de root'un okuyabildiği ayrı dosyada. Biçim: Base64(nonce 12 | şifreli | etiket 16).
/// Anahtar yoksa ya da biçimi bozuksa (Base64 değil / 32 bayt değil) <see cref="IsConfigured"/> false ve hata logu; platform
/// yöneticisi girişi güvenle reddedilir, diğer girişler etkilenmez (review round 1, L-1). Anahtar değeri loglanmaz.
/// </summary>
public sealed class AesGcmSecretProtector : ISecretProtector
{
    private readonly byte[]? key;

    public AesGcmSecretProtector(IConfiguration configuration, ILogger<AesGcmSecretProtector> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        string? encoded = configuration["TwoFactor:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return;
        }

        byte[]? bytes = null;
        try
        {
            bytes = Convert.FromBase64String(encoded.Trim());
        }
        catch (FormatException)
        {
            bytes = null;
        }

        if (bytes is { Length: 32 })
        {
            this.key = bytes;
        }
        else
        {
            logger.LogError("TwoFactor:EncryptionKey geçersiz (32 baytlık Base64 olmalı); iki adımlı doğrulama yapılandırılmamış sayılır");
        }
    }

    public bool IsConfigured => this.key is not null;

    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        byte[] secret = this.Key();
        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(plaintext);
        byte[] cipher = new byte[data.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        using AesGcm aes = new(secret, tag.Length);
        aes.Encrypt(nonce, data, cipher, tag);
        return Convert.ToBase64String([.. nonce, .. cipher, .. tag]);
    }

    public string Unprotect(string cipher)
    {
        ArgumentNullException.ThrowIfNull(cipher);
        byte[] secret = this.Key();
        byte[] all = Convert.FromBase64String(cipher);
        int nonceSize = AesGcm.NonceByteSizes.MaxSize;
        int tagSize = AesGcm.TagByteSizes.MaxSize;
        byte[] data = new byte[all.Length - nonceSize - tagSize];
        using AesGcm aes = new(secret, tagSize);
        aes.Decrypt(all.AsSpan(0, nonceSize), all.AsSpan(nonceSize, data.Length), all.AsSpan(all.Length - tagSize), data);
        return System.Text.Encoding.UTF8.GetString(data);
    }

    private byte[] Key() => this.key ?? throw new InvalidOperationException("İki adımlı doğrulama şifreleme anahtarı ayarlı değil.");
}
