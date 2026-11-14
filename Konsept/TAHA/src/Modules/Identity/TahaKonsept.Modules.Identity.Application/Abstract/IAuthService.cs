using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Kimlik doğrulama akışları (API <c>api/identity/*</c>): kurulum durumu ve tek seferlik kurulum, giriş, token yenileme,
/// tüm oturumlardan çıkış. Tek kullanıcı modu: kayıt yoktur; kullanıcı yalnızca veritabanı boşken kurulumla açılır.
/// </summary>
public interface IAuthService : IApplicationService
{
    /// <summary>Kurulum gerekli mi (hiç kullanıcı yoksa true).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kurulum durumu.</returns>
    Task<Result<SetupStatusResponse>> GetSetupStatusAsync(CancellationToken ct = default);

    /// <summary>Hiç kullanıcı yokken tek yönetici hesabını oluşturur ve web token çifti döner; aksi halde Conflict.</summary>
    /// <param name="request">Kullanıcı adı + parola.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Token çifti.</returns>
    Task<Result<TokenResponse>> SetupAsync(SetupRequest request, CancellationToken ct = default);

    /// <summary>Kullanıcı adı + parola ile giriş; yasaklı / kilitli kullanıcı reddedilir. Başarıda web token çifti.</summary>
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
