using Konsept.Modules.Identity.Infrastructure.Security.Base;
using Microsoft.AspNetCore.Identity;

namespace Konsept.Modules.Identity.Infrastructure.Security.Concrete;

/// <summary>
/// ASP.NET Core Identity'nin <see cref="PasswordHasher{TUser}"/>'ı (V3 biçimi: PBKDF2-HMAC-SHA512, rastgele tuz; paylaşılan
/// çerçevede, yeni paket yok — plan Risk 1). <c>UserManager</c> kullanılmaz.
/// </summary>
public sealed class AspNetPasswordHashing : PasswordHashingBase
{
    private static readonly object Owner = new();
    private readonly PasswordHasher<object> _hasher = new();

    /// <inheritdoc />
    protected override string HashCore(string password) => _hasher.HashPassword(Owner, password);

    /// <inheritdoc />
    protected override bool VerifyCore(string hash, string password)
    {
        try
        {
            return _hasher.VerifyHashedPassword(Owner, hash, password) is not PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
