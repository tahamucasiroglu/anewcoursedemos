using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;

namespace TahaKonsept.Web.Infrastructure.Auth.Base;

/// <summary>
/// <see cref="IPrincipalFactory"/> ortak davranışı: somut sınıfın token'dan okuduğu ham claim'leri
/// (<see cref="ReadClaims"/>) standart türlere eşler (sub → NameIdentifier, name/unique_name → Name, role → Role,
/// permission/permissions → <see cref="AuthTokenNames.PermissionClaim"/>) ve cookie şemasıyla kimlik oluşturur.
/// </summary>
public abstract class PrincipalFactoryBase : IPrincipalFactory
{
    private static readonly string[] NameKeys = ["unique_name", "name", "preferred_username", ClaimTypes.Name];
    private static readonly string[] RoleKeys = ["role", "roles", ClaimTypes.Role];
    private static readonly string[] PermissionKeys = ["permission", "permissions", "perm"];
    private static readonly string[] IdKeys = ["sub", "nameid", ClaimTypes.NameIdentifier];

    /// <inheritdoc />
    public ClaimsPrincipal Create(string accessToken)
    {
        var raw = ReadClaims(accessToken).ToList();
        var claims = new List<Claim>();

        foreach (var claim in raw)
        {
            var type = claim.Type;
            if (IdKeys.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
            }
            else if (NameKeys.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Name, claim.Value));
            }
            else if (RoleKeys.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, claim.Value));
            }
            else if (PermissionKeys.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(AuthTokenNames.PermissionClaim, claim.Value));
            }
        }

        if (!claims.Any(c => c.Type == ClaimTypes.Name))
        {
            claims.Add(new Claim(ClaimTypes.Name, "Kullanıcı"));
        }

        var identity = new ClaimsIdentity(claims.DistinctBy(c => (c.Type, c.Value)), CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>Token'daki ham claim'leri okur (tür adları token'daki gibi).</summary>
    /// <param name="accessToken">Erişim token'ı.</param>
    protected abstract IEnumerable<Claim> ReadClaims(string accessToken);
}
