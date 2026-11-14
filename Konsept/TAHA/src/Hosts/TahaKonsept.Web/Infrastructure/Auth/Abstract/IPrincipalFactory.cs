using System.Security.Claims;

namespace TahaKonsept.Web.Infrastructure.Auth.Abstract;

/// <summary>
/// API'nin verdiği erişim token'ından (JWT) MVC cookie oturumu için <see cref="ClaimsPrincipal"/> kurar
/// (kullanıcı kimliği, adı, roller ve <c>permission</c> claim'leri). İmza doğrulaması API'dedir; burada sadece okunur.
/// </summary>
public interface IPrincipalFactory
{
    /// <summary>Erişim token'ından kimlik oluşturur.</summary>
    /// <param name="accessToken">JWT erişim token'ı.</param>
    ClaimsPrincipal Create(string accessToken);
}
