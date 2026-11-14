using System.Buffers.Text;
using System.Security.Cryptography;
using TahaKonsept.Modules.Identity.Application.Base;

namespace TahaKonsept.Modules.Identity.Application.Concrete;

/// <summary>
/// 64 baytlık kriptografik rastgele değerden base64url refresh token üretir.
/// </summary>
public sealed class RandomRefreshTokenGenerator : RefreshTokenGeneratorBase
{
    private const int ByteLength = 64;

    /// <summary>Yeni ham refresh token üretir.</summary>
    /// <returns>Ham token (base64url, dolgusuz).</returns>
    public override string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(ByteLength));
}
