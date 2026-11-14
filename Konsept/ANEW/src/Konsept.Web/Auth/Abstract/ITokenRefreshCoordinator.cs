using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;

namespace Konsept.Web.Auth.Abstract;

/// <summary>
/// Refresh token dönüşünü tek uçuşa indirir (plan Risk 4): aynı eski refresh token için eşzamanlı istekler (sayfa + 3 sn'lik
/// durum yoklaması) Api'ye tek çağrı yapar ve aynı yeni token çiftini paylaşır; sonuç kısa süre bellekte tutulur.
/// </summary>
public interface ITokenRefreshCoordinator
{
    /// <summary>Refresh token'ı döndürür (gerekirse başka isteğin sonucunu paylaşır).</summary>
    /// <param name="api">API istemcisi (isteğin kapsamından).</param>
    /// <param name="refreshToken">Geçerli refresh token.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni token çifti ya da hata.</returns>
    Task<Result<AuthSessionResponse>> RefreshAsync(IKonseptApiClient api, string refreshToken, CancellationToken cancellationToken);
}
