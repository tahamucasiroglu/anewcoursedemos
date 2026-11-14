using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Dtos;

namespace Konsept.Web.ApiClient.Abstract;

/// <summary>
/// Web'in API'ye yaptığı çağrılar. API'ye ulaşılamazsa <c>api.unavailable</c> hatası döner; 401 → Unauthorized,
/// 429 → TooManyRequests türünde hata.
/// </summary>
public interface IKonseptApiClient
{
    /// <summary>Sistem durumunu getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum ya da hata.</returns>
    Task<Result<SystemStatusResponse>> GetSystemStatusAsync(CancellationToken cancellationToken);

    /// <summary>Ajana test komutu gönderir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ajan bilgisi ya da hata.</returns>
    Task<Result<AgentTestResponse>> TestAgentAsync(CancellationToken cancellationToken);

    /// <summary>Web'in Warning+ loglarını veritabanındaki teknik loga iletir (servis anahtarıyla).</summary>
    /// <param name="logs">Kayıtlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yazılan kayıt sayısı ya da hata.</returns>
    Task<Result<int>> ForwardLogsAsync(IReadOnlyList<ForwardedLogRequest> logs, CancellationToken cancellationToken);

    /// <summary>Kayıt olur; tarayıcının IP'si servis anahtarıyla iletilir (Api'nin IP başına hız sınırı için).</summary>
    /// <param name="request">Kayıt isteği.</param>
    /// <param name="clientIp">Tarayıcının IP adresi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token çifti ya da hata (alan hataları, e-posta kayıtlı, çok fazla deneme).</returns>
    Task<Result<AuthSessionResponse>> RegisterAsync(RegisterApiRequest request, string? clientIp, CancellationToken cancellationToken);

    /// <summary>Giriş yapar; tarayıcının IP'si servis anahtarıyla iletilir.</summary>
    /// <param name="request">Giriş isteği.</param>
    /// <param name="clientIp">Tarayıcının IP adresi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token çifti ya da hata (kalan hak / kilit ek bilgide).</returns>
    Task<Result<AuthSessionResponse>> LoginAsync(LoginApiRequest request, string? clientIp, CancellationToken cancellationToken);

    /// <summary>Refresh token'ı döndürür.</summary>
    /// <param name="refreshToken">Geçerli refresh token.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni token çifti ya da Unauthorized.</returns>
    Task<Result<AuthSessionResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Kullanıcının tüm oturumlarını kapatır.</summary>
    /// <param name="accessToken">Erişim token'ı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İptal edilen oturum sayısı ya da hata.</returns>
    Task<Result<int>> LogoutAsync(string accessToken, CancellationToken cancellationToken);

    /// <summary>Erişim token'ının oturumunu doğrular ve kullanıcıyı getirir.</summary>
    /// <param name="accessToken">Erişim token'ı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcı ya da Unauthorized (oturum iptal/süresi dolmuş).</returns>
    Task<Result<CurrentUserResponse>> GetSessionAsync(string accessToken, CancellationToken cancellationToken);
}
