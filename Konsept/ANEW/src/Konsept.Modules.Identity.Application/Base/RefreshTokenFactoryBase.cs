using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Shared.Kernel.Security;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Refresh token: <see cref="SecretMasker.RefreshTokenPrefix"/> + kriptografik rastgele 32 bayt (base64url). Saklanan biçim
/// SHA-256 hex'tir (token yüksek entropili olduğundan yavaş hash gerekmez). Rastgele kaynak alt sınıfta değiştirilebilir.
/// </summary>
public abstract class RefreshTokenFactoryBase : IRefreshTokenFactory
{
    /// <summary>Rastgele bayt sayısı.</summary>
    public const int TokenBytes = 32;

    /// <inheritdoc />
    public string Create() => SecretMasker.RefreshTokenPrefix + Base64Url.EncodeToString(NextBytes(TokenBytes));

    /// <inheritdoc />
    public string Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    /// <summary>Kriptografik rastgele bayt üretir.</summary>
    /// <param name="count">Bayt sayısı.</param>
    /// <returns>Baytlar.</returns>
    protected abstract byte[] NextBytes(int count);
}
