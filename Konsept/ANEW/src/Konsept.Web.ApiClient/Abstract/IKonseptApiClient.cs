using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Dtos;

namespace Konsept.Web.ApiClient.Abstract;

/// <summary>
/// Web'in API'ye yaptığı çağrılar. API'ye ulaşılamazsa <c>api.unavailable</c> hatası döner; 401 → Unauthorized,
/// 403 → Forbidden, 429 → TooManyRequests türünde hata.
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

    /// <summary>Admin alanının özetini getirir (yalnızca <c>admin.panel</c> yetkisiyle; Api karar verir).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Özet ya da hata (403 → Forbidden).</returns>
    Task<Result<AdminOverviewResponse>> GetAdminOverviewAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ajan eşleştirme (spec 0012): kodun cihazını sorar (<c>lookup</c>), onaylar (<c>approve</c>) ya da reddeder (<c>reject</c>);
    /// tarayıcının IP'si servis anahtarıyla iletilir (Api'nin IP başına hız sınırı, Req 8).
    /// </summary>
    /// <param name="action"><c>lookup</c>, <c>approve</c> ya da <c>reject</c>.</param>
    /// <param name="code">Kullanıcının yazdığı kod.</param>
    /// <param name="clientIp">Tarayıcının IP adresi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz ya da hata (kod yok 404 / süresi dolmuş, kullanılmış 409 / çok fazla deneme 429).</returns>
    Task<Result<PairingDeviceResponse>> AgentPairingAsync(string action, string? code, string? clientIp, CancellationToken cancellationToken);

    /// <summary>Model kataloğunu (kullanıcıya göre etiketli) getirir (spec 0004).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Katalog ya da hata.</returns>
    Task<Result<CatalogResponse>> GetCatalogAsync(CancellationToken cancellationToken);

    /// <summary>Kullanıcının sağlayıcı satırlarını (maskeli key'ler) getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Satırlar ya da hata.</returns>
    Task<Result<IReadOnlyList<ApiKeyStatusResponse>>> GetApiKeysAsync(CancellationToken cancellationToken);

    /// <summary>Key'i doğrulatıp kaydeder (varsa eskisinin yerine).</summary>
    /// <param name="provider">Sağlayıcı kimliği.</param>
    /// <param name="apiKey">Key (yalnızca istek gövdesinde gider).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni satır ya da hata (biçim / sağlayıcı reddi alan hatasında).</returns>
    Task<Result<ApiKeyStatusResponse>> SaveApiKeyAsync(string provider, string? apiKey, CancellationToken cancellationToken);

    /// <summary>Key'i kalıcı siler.</summary>
    /// <param name="provider">Sağlayıcı kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>"Girilmemiş" satırı ya da hata.</returns>
    Task<Result<ApiKeyStatusResponse>> DeleteApiKeyAsync(string provider, CancellationToken cancellationToken);
}
