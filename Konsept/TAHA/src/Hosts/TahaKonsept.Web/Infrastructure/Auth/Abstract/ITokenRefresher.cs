using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Web.Infrastructure.Auth.Abstract;

/// <summary>
/// Refresh token ile yeni token çifti alır (<c>POST api/identity/refresh</c>) ve oturuma yazar.
/// Refresh token'lar dönen (rotating) olduğundan aynı anda gelen istekler tek yenilemede birleştirilir; aksi hâlde
/// eski token'ın ikinci kullanımı API'de token ailesini iptal ettirir.
/// </summary>
public interface ITokenRefresher
{
    /// <summary>
    /// <paramref name="current"/> token'larını yeniler. Başarıda yeni token'lar (oturuma da yazılmış), API reddederse
    /// oturum kapatılır ve null; API'ye ulaşılamazsa oturum korunur ve null döner.
    /// </summary>
    /// <param name="current">Geçerli token'lar.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<TokenResponse?> RefreshAsync(TokenResponse current, CancellationToken ct = default);
}
