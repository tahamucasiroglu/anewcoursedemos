using TahaKonsept.Web.Infrastructure.Api;
using TahaKonsept.Web.Models;

namespace TahaKonsept.Web.Services.Account.Abstract;

/// <summary>
/// Web oturum işlemleri (BFF): kayıt durumu (<see cref="IsRegistrationOpenAsync"/>), kayıt (<see cref="RegisterAsync"/>: normal
/// kullanıcı açar, oturumu açar, Başlangıç'a yönlendirir), giriş (<see cref="LoginAsync"/>: kullanıcı adı ya da e-posta; API'den token al →
/// şifreli cookie'ye yaz) ve çıkış (<see cref="LogoutAsync"/>: API'de tüm refresh token'ları iptal et, ajan dahil → cookie'yi sil).
/// </summary>
public interface IAccountService
{
    /// <summary>Yeni kayıt açık mı (API'ye ulaşılamazsa true; kayıt denemesi zaten API'de reddedilir).</summary>
    /// <param name="ct">İptal belirteci.</param>
    Task<bool> IsRegistrationOpenAsync(CancellationToken ct = default);

    /// <summary>Kullanıcı adı (ya da e-posta) / parola ile giriş yapar; başarıda dönülecek adresi verir.</summary>
    /// <param name="request">Giriş isteği.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<RedirectResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Yeni hesap açar; API token dönerse oturumu da açar ve Başlangıç'a yönlendirir.</summary>
    /// <param name="request">Kayıt isteği.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<RedirectResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>Tüm oturumları kapatır (API'de logout-all + cookie silme).</summary>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<RedirectResponse>> LogoutAsync(CancellationToken ct = default);
}
