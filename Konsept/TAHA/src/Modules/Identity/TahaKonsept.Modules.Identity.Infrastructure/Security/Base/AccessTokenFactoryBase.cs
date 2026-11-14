using System.Security.Claims;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Enums;

namespace TahaKonsept.Modules.Identity.Infrastructure.Security.Base;

/// <summary>
/// <see cref="IAccessTokenFactory"/> tabanı: claim listesini (<c>sub</c>, <c>name</c>, <c>jti</c>, <c>role</c>*, <c>permission</c>*,
/// <c>tfam</c>, <c>client</c>) ve süreyi (<see cref="JwtOptions.AccessTokenMinutes"/>) hazırlar; imzalama türetilen sınıfta (<see cref="Sign"/>).
/// </summary>
public abstract class AccessTokenFactoryBase : IAccessTokenFactory
{
    /// <summary>Rol claim adı (host <c>RoleClaimType = "role"</c>).</summary>
    public const string RoleClaimType = "role";

    /// <summary>JWT kimlik claim adı.</summary>
    public const string TokenIdClaimType = "jti";

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="options">JWT ayarları.</param>
    /// <param name="clock">Saat.</param>
    protected AccessTokenFactoryBase(JwtOptions options, IClock clock)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>JWT ayarları.</summary>
    protected JwtOptions Options { get; }

    /// <summary>Saat.</summary>
    protected IClock Clock { get; }

    /// <summary>Erişim token'ı üretir.</summary>
    /// <param name="descriptor">Token içeriği.</param>
    /// <returns>Token + bitiş anı.</returns>
    public AccessTokenResult Create(AccessTokenDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var issuedAt = Clock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(Math.Max(1, Options.AccessTokenMinutes));

        var claims = new List<Claim>
        {
            new(CustomClaimTypes.UserId, descriptor.UserId.ToString()),
            new(CustomClaimTypes.UserName, descriptor.UserName),
            new(TokenIdClaimType, Guid.NewGuid().ToString("N")),
            new(CustomClaimTypes.TokenFamily, descriptor.FamilyId.ToString()),
            new(CustomClaimTypes.ClientType, descriptor.Kind == RefreshTokenKind.Agent ? "agent" : "web"),
        };
        claims.AddRange(descriptor.Roles.Select(r => new Claim(RoleClaimType, r)));
        claims.AddRange(descriptor.Permissions.Select(p => new Claim(CustomClaimTypes.Permission, p)));

        return new AccessTokenResult(Sign(claims, issuedAt, expiresAt), expiresAt);
    }

    /// <summary>Claim'leri imzalı token'a çevirir.</summary>
    /// <param name="claims">Claim'ler.</param>
    /// <param name="issuedAtUtc">Veriliş anı.</param>
    /// <param name="expiresAtUtc">Bitiş anı.</param>
    /// <returns>Token metni.</returns>
    protected abstract string Sign(IReadOnlyList<Claim> claims, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc);
}
