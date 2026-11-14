using System.Security.Cryptography;
using Konsept.Modules.Identity.Application.Base;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Refresh token üretimi; rastgele kaynak <see cref="RandomNumberGenerator"/>.</summary>
public sealed class RefreshTokenFactory : RefreshTokenFactoryBase
{
    /// <inheritdoc />
    protected override byte[] NextBytes(int count) => RandomNumberGenerator.GetBytes(count);
}
