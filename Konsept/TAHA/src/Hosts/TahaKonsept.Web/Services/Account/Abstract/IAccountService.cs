using TahaKonsept.Web.Infrastructure.Api;
using TahaKonsept.Web.Models;

namespace TahaKonsept.Web.Services.Account.Abstract;

/// <summary>
/// Web oturum işlemleri (BFF): kurulum durumu (<see cref="IsSetupRequiredAsync"/>), ilk yönetici hesabı
/// (<see cref="SetupAsync"/>), giriş (<see cref="LoginAsync"/>: API'den token al → şifreli cookie'ye yaz) ve çıkış
/// (<see cref="LogoutAsync"/>: API'de tüm refresh token'ları iptal et, ajan dahil → cookie'yi sil).
/// </summary>
public interface IAccountService
{
    /// <summary>Veritabanında hiç kullanıcı yoksa true (API'ye ulaşılamazsa false).</summary>
    /// <param name="ct">İptal belirteci.</param>
    Task<bool> IsSetupRequiredAsync(CancellationToken ct = default);

    /// <summary>Kullanıcı adı/parola ile giriş yapar; başarıda dönülecek adresi verir.</summary>
    /// <param name="request">Giriş isteği.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<RedirectResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>İlk yönetici hesabını oluşturur; API token dönerse oturumu da açar.</summary>
    /// <param name="request">Kurulum isteği.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<RedirectResponse>> SetupAsync(SetupRequest request, CancellationToken ct = default);

    /// <summary>Tüm oturumları kapatır (API'de logout-all + cookie silme).</summary>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<RedirectResponse>> LogoutAsync(CancellationToken ct = default);
}
