using System.Text;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Security.Concrete;

/// <summary>
/// JWT imzalama anahtarı: <see cref="JwtOptions.SigningKey"/> doluysa (base64 ya da ≥32 karakter metin) onu,
/// değilse <c>App_Data/keys/jwt.key</c> dosyasını (yoksa 64 bayt üretir) kullanır. Anahtar ilk istekte bir kez yüklenir.
/// </summary>
public sealed class JwtSigningKeyProvider : IJwtSigningKeyProvider
{
    private const int MinimumKeyBytes = 32;
    private readonly Lazy<byte[]> _key;

    /// <summary>Oluşturur.</summary>
    /// <param name="options">JWT ayarları.</param>
    /// <param name="appData">Veri klasörü.</param>
    public JwtSigningKeyProvider(JwtOptions options, IAppDataLocation appData)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(appData);
        _key = new Lazy<byte[]>(() => Load(options, appData), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>İmzalama anahtarı.</summary>
    /// <returns>Anahtar baytları.</returns>
    public byte[] GetSigningKey() => _key.Value;

    private static byte[] Load(JwtOptions options, IAppDataLocation appData)
    {
        var configured = options.SigningKey?.Trim();
        if (string.IsNullOrEmpty(configured))
        {
            return KeyFileStore.GetOrCreateKey(appData.GetPath("keys", "jwt.key"), 64);
        }

        var buffer = new byte[configured.Length];
        if (Convert.TryFromBase64String(configured, buffer, out var written) && written >= MinimumKeyBytes)
        {
            return buffer[..written];
        }

        var raw = Encoding.UTF8.GetBytes(configured);
        if (raw.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException($"Jwt:SigningKey en az {MinimumKeyBytes} bayt olmalıdır.");
        }

        return raw;
    }
}
