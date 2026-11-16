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

    /// <summary>Bu ayki (İstanbul) harcama toplamını getirir (spec 0013).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Toplam ya da hata.</returns>
    Task<Result<MonthlySpendResponse>> GetMonthlySpendAsync(CancellationToken cancellationToken);

    /// <summary>Bir metin modeliyle deneme çağrısı yapar (plan 0013; kapının tüm kurallarıyla, maliyet kaydedilir).</summary>
    /// <param name="modelId">Model kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç ya da Türkçe nedenli hata.</returns>
    Task<Result<TrialCallResponse>> TrialCallAsync(string? modelId, CancellationToken cancellationToken);

    /// <summary>Profiller, seçili profil, sınır ve şablonlar (spec 0003).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste ya da hata.</returns>
    Task<Result<ProfileListResponse>> GetProfilesAsync(CancellationToken cancellationToken);

    /// <summary>Profil oluşturur (yeni profil seçili olur).</summary>
    /// <param name="request">Alanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da hata (alan hataları / sınır).</returns>
    Task<Result<ProfileResponse>> CreateProfileAsync(SaveProfileApiRequest? request, CancellationToken cancellationToken);

    /// <summary>Profili düzenler.</summary>
    /// <param name="id">Profil.</param>
    /// <param name="request">Alanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da hata.</returns>
    Task<Result<ProfileResponse>> UpdateProfileAsync(Guid id, SaveProfileApiRequest? request, CancellationToken cancellationToken);

    /// <summary>Profili kalıcı siler.</summary>
    /// <param name="id">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni liste ya da hata.</returns>
    Task<Result<ProfileListResponse>> DeleteProfileAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Seçili profili değiştirir (sunucuda hatırlanır).</summary>
    /// <param name="id">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Seçilen profil ya da hata.</returns>
    Task<Result<ProfileResponse>> SelectProfileAsync(Guid? id, CancellationToken cancellationToken);

    /// <summary>Profilin Konseptler sayfası ayarları ve seçilebilir modeller (spec 0005).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar ya da hata (404 başka kullanıcının profili).</returns>
    Task<Result<ConceptSettingsResponse>> GetConceptSettingsAsync(Guid profileId, CancellationToken cancellationToken);

    /// <summary>Profilin Konseptler sayfası ayarlarını kaydeder (spec 0005).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="request">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni ayarlar ya da hata (alan hataları / 404).</returns>
    Task<Result<ConceptSettingsResponse>> SaveConceptSettingsAsync(Guid profileId, SaveConceptSettingsApiRequest? request, CancellationToken cancellationToken);

    /// <summary>Günlük geçmişinde bir günün bir sayfasını getirir (spec 0021).</summary>
    /// <param name="date">Gün (<c>yyyy-MM-dd</c>); boşsa bugün.</param>
    /// <param name="filter">Seviye filtresi (<c>all</c>, <c>warning</c>, <c>error</c>).</param>
    /// <param name="skip">Atlanacak kayıt sayısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gün sayfası ya da hata.</returns>
    Task<Result<JournalDayResponse>> GetJournalDayAsync(string? date, string? filter, int skip, CancellationToken cancellationToken);

    /// <summary>"Hata bildir" modalının otomatik bilgileri (spec 0022 Req 1).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Önizleme ya da hata.</returns>
    Task<Result<ErrorReportPreviewResponse>> GetErrorReportPreviewAsync(CancellationToken cancellationToken);

    /// <summary>Web'den hata bildirimi gönderir (spec 0022 Req 1–2).</summary>
    /// <param name="request">Açıklama, sayfa, korelasyon.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bildirim numarası ya da hata (doğrulama, sınır).</returns>
    Task<Result<ErrorReportReceiptResponse>> SubmitErrorReportAsync(ErrorReportApiRequest? request, CancellationToken cancellationToken);
}
