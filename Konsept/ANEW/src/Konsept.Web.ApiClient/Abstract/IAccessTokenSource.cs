namespace Konsept.Web.ApiClient.Abstract;

/// <summary>
/// Geçerli isteğin Api erişim token'ı (Web'de şifreli cookie'deki oturum biletinden). API istemcisi her çağrıya bunu
/// Bearer olarak ekler.
/// </summary>
public interface IAccessTokenSource
{
    /// <summary>Geçerli erişim token'ını döndürür.</summary>
    /// <returns>Token; giriş yapılmamışsa <c>null</c>.</returns>
    string? GetAccessToken();
}
