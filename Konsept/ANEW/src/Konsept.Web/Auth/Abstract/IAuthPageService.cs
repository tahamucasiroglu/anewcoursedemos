using Konsept.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Auth.Abstract;

/// <summary>
/// Giriş, kayıt ve çıkış sayfalarının mantığı (controller tek satır kalır, K-4): Api'ye sorar, başarıda oturum cookie'sini
/// yazar ve güvenli adrese yönlendirir; hatada formu Türkçe mesajlarla yeniden gösterir.
/// </summary>
public interface IAuthPageService
{
    /// <summary>Giriş sayfası; giriş yapmışsa Konseptler'e yönlendirir.</summary>
    /// <param name="controller">Çağıran controller.</param>
    /// <param name="returnUrl">Dönüş adresi.</param>
    /// <param name="sessionEnded">Oturum sona erdiği için mi gelindi.</param>
    /// <returns>Sonuç.</returns>
    IActionResult ShowLogin(Controller controller, string? returnUrl, bool sessionEnded);

    /// <summary>Giriş formunu işler.</summary>
    /// <param name="controller">Çağıran controller.</param>
    /// <param name="form">Form.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yönlendirme ya da hatalı form.</returns>
    Task<IActionResult> LoginAsync(Controller controller, LoginForm form, CancellationToken cancellationToken);

    /// <summary>Kayıt sayfası; giriş yapmışsa Konseptler'e yönlendirir.</summary>
    /// <param name="controller">Çağıran controller.</param>
    /// <param name="returnUrl">Dönüş adresi.</param>
    /// <returns>Sonuç.</returns>
    IActionResult ShowRegister(Controller controller, string? returnUrl);

    /// <summary>Kayıt formunu işler.</summary>
    /// <param name="controller">Çağıran controller.</param>
    /// <param name="form">Form.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yönlendirme ya da hatalı form.</returns>
    Task<IActionResult> RegisterAsync(Controller controller, RegisterForm form, CancellationToken cancellationToken);

    /// <summary>Çıkış: Api'de tüm oturumları kapatır, cookie'yi siler, giriş sayfasına götürür.</summary>
    /// <param name="controller">Çağıran controller.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yönlendirme.</returns>
    Task<IActionResult> LogoutAsync(Controller controller, CancellationToken cancellationToken);
}
