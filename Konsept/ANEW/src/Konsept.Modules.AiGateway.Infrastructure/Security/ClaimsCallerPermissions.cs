using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Http;

namespace Konsept.Modules.AiGateway.Infrastructure.Security;

/// <summary>
/// İsteği yapan kullanıcının yetkilerini Api principal'ındaki <see cref="AuthPolicies.PermissionClaim"/> claim'lerinden okur. Bu
/// claim'ler her istekte sunucuda rolden kurulur; token'dan gelen aynı adlı claim'ler önce silinir (plan 0011, 0013). Giriş yoksa boş.
/// </summary>
/// <param name="accessor">HTTP bağlamı erişimcisi.</param>
public sealed class ClaimsCallerPermissions(IHttpContextAccessor accessor) : ICallerPermissions
{
    /// <inheritdoc />
    public IReadOnlyCollection<string> Current =>
        accessor.HttpContext?.User is { Identity.IsAuthenticated: true } user
            ? [.. user.FindAll(AuthPolicies.PermissionClaim).Select(c => c.Value)]
            : [];
}
