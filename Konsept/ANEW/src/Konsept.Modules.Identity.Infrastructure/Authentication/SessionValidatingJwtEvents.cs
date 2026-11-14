using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Shared.Infrastructure.Http.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Konsept.Modules.Identity.Infrastructure.Authentication;

/// <summary>
/// İmzası ve süresi geçerli her JWT'nin oturumunu (<c>sid</c>) veritabanından doğrular: iptal edilmiş ya da süresi dolmuş
/// oturumun token'ı 15 dakikası dolmadan da reddedilir — çıkış diğer tarayıcıyı anında düşürür (spec 0002 AC-14).
/// Çerçeve adaptörü; karar <see cref="ISessionService.ValidateAsync"/>'tedir.
/// </summary>
/// <param name="sessions">Oturum servisi.</param>
public sealed class SessionValidatingJwtEvents(ISessionService sessions) : JwtBearerEvents
{
    /// <inheritdoc />
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sid = context.Principal?.FindFirst(CurrentUserBase.SessionClaim)?.Value;
        if (!Guid.TryParse(sid, out var sessionId)
            || !await sessions.ValidateAsync(sessionId, context.HttpContext.RequestAborted).ConfigureAwait(false))
        {
            context.Fail("Oturum geçersiz.");
        }
    }
}
