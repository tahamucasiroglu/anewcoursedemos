using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OkulYonetim.Infrastructure.Security;

/// <summary>
/// Ortak AES-GCM çekirdeği (spec 0007'den ayrıldı, spec 0009 ile paylaşılır): ayardan 32 baytlık Base64 anahtarı okur
/// (yoksa ya da bozuksa anahtarsız, hata logu — anahtar değeri loglanmaz) ve metni şifreler/çözer. Biçim:
/// Base64(nonce 12 | şifreli | etiket 16); her şifrelemede yeni rastgele nonce. Bozulmuş ya da başka anahtarla şifrelenmiş
/// veri <see cref="CryptographicException"/> verir.
/// </summary>
public static class AesGcmCipher
{
    private const int KeySize = 32;

    /// <summary>Anahtarı ayardan okur; boşsa sessizce, geçersizse hata loguyla <c>null</c>.</summary>
    public static byte[]? ReadKey(IConfiguration configuration, string settingName, ILogger logger, string feature)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        string? encoded = configuration[settingName];
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return null;
        }

        byte[]? bytes;
        try
        {
            bytes = Convert.FromBase64String(encoded.Trim());
        }
        catch (FormatException)
        {
            bytes = null;
        }

        if (bytes is { Length: KeySize })
        {
            return bytes;
        }

        logger.LogError("{Setting:l} geçersiz (32 baytlık Base64 olmalı); {Feature:l} yapılandırılmamış sayılır", settingName, feature);
        return null;
    }

    public static string Encrypt(byte[] key, string plaintext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(plaintext);
        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(plaintext);
        byte[] cipher = new byte[data.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        using AesGcm aes = new(key, tag.Length);
        aes.Encrypt(nonce, data, cipher, tag);
        return Convert.ToBase64String([.. nonce, .. cipher, .. tag]);
    }

    public static string Decrypt(byte[] key, string cipher)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(cipher);
        byte[] all = Convert.FromBase64String(cipher);
        int nonceSize = AesGcm.NonceByteSizes.MaxSize;
        int tagSize = AesGcm.TagByteSizes.MaxSize;
        if (all.Length < nonceSize + tagSize)
        {
            throw new CryptographicException("Şifreli veri biçimi geçersiz.");
        }

        byte[] data = new byte[all.Length - nonceSize - tagSize];
        using AesGcm aes = new(key, tagSize);
        aes.Decrypt(all.AsSpan(0, nonceSize), all.AsSpan(nonceSize, data.Length), all.AsSpan(all.Length - tagSize), data);
        return System.Text.Encoding.UTF8.GetString(data);
    }
}
