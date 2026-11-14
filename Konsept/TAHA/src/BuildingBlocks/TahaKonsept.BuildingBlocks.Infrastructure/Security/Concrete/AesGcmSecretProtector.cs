using System.Security.Cryptography;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Security.Base;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Security.Concrete;

/// <summary>
/// AES-256-GCM gizli değer koruyucu. Paket biçimi: <c>nonce(12) | tag(16) | ciphertext</c>.
/// Master key: <c>TAHAKONSEPT_MASTER_KEY</c> ortam değişkeni (base64, 32 bayt) ya da <c>App_Data/keys/master.key</c> (ilk açılışta üretilir).
/// Anahtar kaybolursa kayıtlı API key'ler çözülemez — yedeklenmelidir.
/// </summary>
public sealed class AesGcmSecretProtector : SecretProtectorBase
{
    /// <summary>Master key ortam değişkeni adı.</summary>
    public const string MasterKeyEnvironmentVariable = "TAHAKONSEPT_MASTER_KEY";

    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    /// <summary>Anahtarı ortamdan ya da veri klasöründeki dosyadan yükler.</summary>
    /// <param name="appData">Veri klasörü.</param>
    public AesGcmSecretProtector(IAppDataLocation appData)
        : this(LoadMasterKey(appData))
    {
    }

    /// <summary>Verilen anahtarla oluşturur (testler için).</summary>
    /// <param name="key">32 baytlık anahtar.</param>
    public AesGcmSecretProtector(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != KeySize)
        {
            throw new ArgumentException($"Master key {KeySize} bayt olmalıdır.", nameof(key));
        }

        _key = (byte[])key.Clone();
    }

    /// <summary>Baytları AES-GCM ile şifreler.</summary>
    /// <param name="plaintext">Düz baytlar.</param>
    /// <returns>Şifreli paket.</returns>
    protected override byte[] Encrypt(byte[] plaintext)
    {
        var payload = new byte[NonceSize + TagSize + plaintext.Length];
        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, cipher, tag);
        return payload;
    }

    /// <summary>AES-GCM paketini çözer (bütünlük doğrulanır).</summary>
    /// <param name="payload">Şifreli paket.</param>
    /// <returns>Düz baytlar.</returns>
    protected override byte[] Decrypt(byte[] payload)
    {
        if (payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Şifreli paket çok kısa.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[cipher.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plaintext);
        return plaintext;
    }

    private static byte[] LoadMasterKey(IAppDataLocation appData)
    {
        ArgumentNullException.ThrowIfNull(appData);
        var fromEnv = Environment.GetEnvironmentVariable(MasterKeyEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            var key = Convert.FromBase64String(fromEnv.Trim());
            if (key.Length != KeySize)
            {
                throw new InvalidOperationException($"{MasterKeyEnvironmentVariable} base64 olarak {KeySize} bayt olmalıdır.");
            }

            return key;
        }

        return KeyFileStore.GetOrCreateKey(appData.GetPath("keys", "master.key"), KeySize);
    }
}
