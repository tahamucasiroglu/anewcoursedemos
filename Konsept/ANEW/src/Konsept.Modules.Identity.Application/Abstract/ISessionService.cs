using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Oturum yaşam döngüsü (spec 0002 Req 13, 15): refresh token dönüşü, her istekte oturum doğrulama (kayan 14 gün),
/// çıkış (tüm oturumları iptal) ve geçerli kullanıcı.
/// </summary>
public interface ISessionService : ISessionRevoker
{
    /// <summary>Refresh token'ı döndürür ve yeni erişim token'ı verir.</summary>
    /// <param name="request">Refresh isteği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni token çifti ya da <c>identity.session_invalid</c>.</returns>
    Task<Result<AuthSessionDto>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);

    /// <summary>Oturum hâlâ geçerli mi (iptal edilmemiş, süresi dolmamış); geçerliyse son kullanımı kaydırır.</summary>
    /// <param name="sessionId">Oturum (<c>sid</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    Task<bool> ValidateAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>Geçerli kullanıcının tüm oturumlarını kapatır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İptal edilen oturum sayısı.</returns>
    Task<Result<int>> LogoutAsync(CancellationToken cancellationToken);

    /// <summary>Geçerli oturumun kullanıcısını döndürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcı ya da <c>identity.session_invalid</c>.</returns>
    Task<Result<CurrentUserDto>> GetCurrentAsync(CancellationToken cancellationToken);
}
