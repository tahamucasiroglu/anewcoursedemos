using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Kimlik doğrulama akışları (API <c>api/identity/*</c>): kayıt durumu + kayıt (her zaman normal kullanıcı; yönetici hesabı
/// <see cref="IAdminSeeder"/> ile yapılandırmadan tohumlanır, yönetici kayıtları kapatabilir), kullanıcı adı ya da e-posta ile giriş,
/// token yenileme, tüm oturumlardan çıkış.
/// </summary>
public interface IAuthService : IApplicationService
{
    /// <summary>Kayıt durumu: yeni kayıt açık mı.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kayıt durumu.</returns>
    Task<Result<RegistrationStatusResponse>> GetRegistrationStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Yeni normal kullanıcı hesabı açar ve web token çifti döner; yönetici kayıtları kapattıysa Forbidden
    /// (<c>identity.registration_closed</c>). Hiç kullanıcı yokken de çalışır (ilk kayıt yönetici OLMAZ).
    /// </summary>
    /// <param name="request">Kullanıcı adı + parola.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Token çifti.</returns>
    Task<Result<TokenResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>
    /// Kullanıcı adı ya da e-posta + parola ile giriş; yasaklı (devre dışı) / kilitli kullanıcı reddedilir. Başarıda web token çifti.
    /// </summary>
    /// <param name="request">Giriş bilgileri.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Token çifti.</returns>
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Refresh token'ı döndürür (rotasyon); tekrar kullanım tespit edilirse aile iptal edilir.</summary>
    /// <param name="request">Mevcut refresh token.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yeni token çifti.</returns>
    Task<Result<TokenResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default);

    /// <summary>Oturumdaki kullanıcının TÜM refresh token'larını (ajan dahil) iptal eder ve bildirimcileri çağırır.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> LogoutAllAsync(CancellationToken ct = default);
}
