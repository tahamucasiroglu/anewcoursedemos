using System.Security.Cryptography;
using System.Text;
using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Security.Base;

/// <summary>
/// <see cref="ISecretProtector"/> ortak davranışı: metin ↔ bayt dönüşümü, sürüm öneki (<see cref="VersionPrefix"/>),
/// maskeleme ve <see cref="TryUnprotect"/>. Türetilen sınıf yalnızca <see cref="Encrypt"/> / <see cref="Decrypt"/> yazar.
/// </summary>
public abstract class SecretProtectorBase : ISecretProtector
{
    /// <summary>Şifreli metin sürüm öneki.</summary>
    protected const string VersionPrefix = "v1:";

    /// <summary>Düz metni şifreler.</summary>
    /// <param name="plaintext">Düz metin.</param>
    /// <returns><c>v1:</c> önekli base64 şifreli metin.</returns>
    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var cipher = Encrypt(Encoding.UTF8.GetBytes(plaintext));
        return VersionPrefix + Convert.ToBase64String(cipher);
    }

    /// <summary>Şifreli metni çözer.</summary>
    /// <param name="protectedValue">Şifreli metin.</param>
    /// <returns>Düz metin.</returns>
    public string Unprotect(string protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);
        if (!protectedValue.StartsWith(VersionPrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("Bilinmeyen şifreli değer biçimi.");
        }

        var payload = Convert.FromBase64String(protectedValue[VersionPrefix.Length..]);
        return Encoding.UTF8.GetString(Decrypt(payload));
    }

    /// <summary>Şifreli metni çözmeyi dener.</summary>
    /// <param name="protectedValue">Şifreli metin.</param>
    /// <param name="plaintext">Düz metin (başarısızsa boş).</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    public bool TryUnprotect(string protectedValue, out string plaintext)
    {
        try
        {
            plaintext = Unprotect(protectedValue);
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
        {
            plaintext = string.Empty;
            return false;
        }
    }

    /// <summary>Son 4 karakteri açık bırakan maske üretir.</summary>
    /// <param name="plaintext">Düz metin.</param>
    /// <returns>Maske (ör. <c>••••abcd</c>).</returns>
    public string Mask(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return string.Empty;
        }

        return plaintext.Length <= 4 ? new string('•', 4) : "••••" + plaintext[^4..];
    }

    /// <summary>Baytları şifreler.</summary>
    /// <param name="plaintext">Düz baytlar.</param>
    /// <returns>Şifreli paket.</returns>
    protected abstract byte[] Encrypt(byte[] plaintext);

    /// <summary>Şifreli paketi çözer.</summary>
    /// <param name="payload">Şifreli paket.</param>
    /// <returns>Düz baytlar.</returns>
    protected abstract byte[] Decrypt(byte[] payload);
}
