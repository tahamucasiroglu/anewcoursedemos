using System.Security.Claims;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Shared.Infrastructure.Http;
using Konsept.Shared.Infrastructure.Http.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Konsept.Modules.Identity.Infrastructure.Authentication;

/// <summary>
/// İmzası ve süresi geçerli her JWT'nin oturumunu (<c>sid</c>) veritabanından doğrular: iptal edilmiş ya da süresi dolmuş
/// oturumun token'ı 15 dakikası dolmadan da reddedilir — çıkış diğer tarayıcıyı anında düşürür (spec 0002 AC-14).
/// Aynı sorgu kullanıcının o anki rolünü döndürür; principal'ın yetki claim'leri (<see cref="AuthPolicies.PermissionClaim"/>)
/// yalnızca bu rolden kurulur, token'dan gelen aynı adlı claim'ler silinir (spec 0011, plan Risk 1). Rol değişikliği bir
/// sonraki istekte geçerlidir. Çerçeve adaptörü; karar <see cref="ISessionService.ValidateSessionAsync"/>'tedir.
/// </summary>
/// <param name="sessions">Oturum servisi.</param>
public sealed class SessionValidatingJwtEvents(ISessionService sessions) : JwtBearerEvents
{
    /// <inheritdoc />
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sid = context.Principal?.FindFirst(CurrentUserBase.SessionClaim)?.Value;
        if (!Guid.TryParse(sid, out var sessionId))
        {
            context.Fail("Oturum geçersiz.");
            return;
        }

        var validation = await sessions.ValidateSessionAsync(sessionId, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!validation.IsValid || context.Principal!.Identity is not ClaimsIdentity identity)
        {
            context.Fail("Oturum geçersiz.");
            return;
        }

        foreach (var owner in context.Principal.Identities)
        {
            foreach (var forged in owner.FindAll(AuthPolicies.PermissionClaim).ToList())
            {
                owner.RemoveClaim(forged);
            }
        }

        foreach (var permission in RolePermissions.For(validation.Role))
        {
            identity.AddClaim(new Claim(AuthPolicies.PermissionClaim, permission));
        }
    }
}
