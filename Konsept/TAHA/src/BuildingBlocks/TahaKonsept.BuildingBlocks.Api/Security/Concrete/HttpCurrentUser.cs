using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;

namespace TahaKonsept.BuildingBlocks.Api.Security.Concrete;

/// <summary>
/// <see cref="ICurrentUser"/>'ın HTTP uygulaması: JWT claim'lerinden (<c>sub</c>, <c>name</c>, <c>permission</c>) okur.
/// Host <c>MapInboundClaims=false</c> olduğundan claim adları JWT'deki gibidir; yine de <see cref="ClaimTypes.NameIdentifier"/> yedek olarak denenir.
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>Oluşturur.</summary>
    /// <param name="accessor">HttpContext erişimcisi.</param>
    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    /// <summary>Kullanıcı kimliği.</summary>
    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirst(CustomClaimTypes.UserId)?.Value
                ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    /// <summary>Kullanıcı adı.</summary>
    public string? UserName => Principal?.FindFirst(CustomClaimTypes.UserName)?.Value ?? Principal?.Identity?.Name;

    /// <summary>Kimliği doğrulanmış mı.</summary>
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <summary>İzinler.</summary>
    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll(CustomClaimTypes.Permission).Select(c => c.Value).Distinct(StringComparer.Ordinal).ToArray() ?? [];

    /// <summary>İzin kontrolü.</summary>
    /// <param name="permission">İzin adı.</param>
    /// <returns>Sahipse <c>true</c>.</returns>
    public bool HasPermission(string permission) =>
        Principal?.HasClaim(CustomClaimTypes.Permission, permission) == true;

    /// <summary>Kullanıcı kimliğini döner; oturum yoksa hata fırlatır.</summary>
    /// <returns>Kimlik.</returns>
    public Guid GetRequiredUserId() =>
        UserId ?? throw new InvalidOperationException("Oturum açmış kullanıcı yok.");
}
