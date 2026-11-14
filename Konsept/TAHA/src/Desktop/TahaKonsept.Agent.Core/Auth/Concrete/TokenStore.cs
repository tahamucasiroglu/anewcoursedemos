using System.Security.Cryptography;
using System.Text;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Auth.Base;

namespace TahaKonsept.Agent.Core.Auth.Concrete;

/// <summary>
/// DPAPI (<see cref="DataProtectionScope.CurrentUser"/>) ile şifreleyen token deposu; dosyayı yalnızca aynı Windows
/// kullanıcısı çözebilir. Windows dışı platformlarda <see cref="PlatformNotSupportedException"/> fırlatır.
/// </summary>
public sealed class TokenStore : TokenStoreBase
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TahaKonsept.Agent.Tokens.v1");

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    public TokenStore(IAgentPaths paths)
        : base(paths)
    {
    }

    /// <inheritdoc />
    protected override byte[] Protect(byte[] plain)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Token şifreleme (DPAPI) yalnızca Windows'ta desteklenir.");
        }

        return ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
    }

    /// <inheritdoc />
    protected override byte[] Unprotect(byte[] cipher)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Token şifreleme (DPAPI) yalnızca Windows'ta desteklenir.");
        }

        return ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
    }
}
