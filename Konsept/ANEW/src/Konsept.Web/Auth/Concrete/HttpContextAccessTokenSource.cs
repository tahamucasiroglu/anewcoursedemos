using Konsept.Web.Auth.Base;

namespace Konsept.Web.Auth.Concrete;

/// <summary>Erişim token'ını geçerli HTTP isteğinden okur (tarayıcı token'ı hiç görmez).</summary>
/// <param name="accessor">HTTP bağlamı erişimcisi.</param>
public sealed class HttpContextAccessTokenSource(IHttpContextAccessor accessor) : AccessTokenSourceBase
{
    /// <inheritdoc />
    protected override IDictionary<object, object?>? Items => accessor.HttpContext?.Items;
}
