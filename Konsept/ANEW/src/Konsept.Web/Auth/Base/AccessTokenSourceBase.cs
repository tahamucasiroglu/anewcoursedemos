using Konsept.Web.ApiClient.Abstract;

namespace Konsept.Web.Auth.Base;

/// <summary>
/// Geçerli isteğin erişim token'ı: oturum doğrulandıktan (gerekirse yenilendikten) sonra isteğe bırakılan değer
/// (<see cref="KonseptCookieEvents.AccessTokenItem"/>). Kaynak (HTTP bağlamı) alt sınıftadır.
/// </summary>
public abstract class AccessTokenSourceBase : IAccessTokenSource
{
    /// <inheritdoc />
    public string? GetAccessToken() => Items?.TryGetValue(KonseptCookieEvents.AccessTokenItem, out var token) == true ? token as string : null;

    /// <summary>Gets isteğin öğe sözlüğü; istek yoksa <c>null</c>.</summary>
    protected abstract IDictionary<object, object?>? Items { get; }
}
