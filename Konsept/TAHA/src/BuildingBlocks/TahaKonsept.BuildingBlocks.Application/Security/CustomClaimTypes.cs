namespace TahaKonsept.BuildingBlocks.Application.Security;

/// <summary>
/// JWT içinde kullanılan claim adları. Host <c>MapInboundClaims=false</c> ile çalışır; yani claim adları JWT'deki gibi kalır.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>Kullanıcı kimliği (JWT <c>sub</c>).</summary>
    public const string UserId = "sub";

    /// <summary>Kullanıcı adı.</summary>
    public const string UserName = "name";

    /// <summary>İzin claim'i (birden çok olabilir; ör. <c>models.use</c>).</summary>
    public const string Permission = "permission";

    /// <summary>Token ailesi (refresh token rotasyonu; ajan ayrı aile).</summary>
    public const string TokenFamily = "tfam";

    /// <summary>İstemci türü (<c>web</c> / <c>agent</c>).</summary>
    public const string ClientType = "client";
}
