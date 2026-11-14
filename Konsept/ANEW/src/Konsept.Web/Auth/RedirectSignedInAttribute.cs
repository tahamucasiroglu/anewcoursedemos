using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Konsept.Web.Auth;

/// <summary>
/// Giriş yapmış kullanıcıyı giriş/kayıt formu gönderiminde, CSRF doğrulamasından önce Konseptler'e yönlendirir (review R2-3):
/// başka sekmede giriş yapıldıktan sonra anonim çizilmiş form gönderilince ham 400 görünmez (spec 0002 Req 12). Çerçeve
/// adaptörü (MVC filter); <see cref="Order"/> CSRF filtresinden (1000) önce çalışır.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RedirectSignedInAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
{
    /// <inheritdoc />
    public int Order => -1000;

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new LocalRedirectResult("/");
        }
    }
}
