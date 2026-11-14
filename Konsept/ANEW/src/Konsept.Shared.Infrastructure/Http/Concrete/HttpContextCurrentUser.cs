using System.Security.Claims;
using Konsept.Shared.Infrastructure.Http.Base;
using Microsoft.AspNetCore.Http;

namespace Konsept.Shared.Infrastructure.Http.Concrete;

/// <summary>İsteği yapan kullanıcıyı geçerli HTTP bağlamından okur (JWT ile doğrulanmış kimlik).</summary>
/// <param name="accessor">HTTP bağlamı erişimcisi.</param>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : CurrentUserBase
{
    /// <inheritdoc />
    protected override ClaimsPrincipal? Principal => accessor.HttpContext?.User;
}
