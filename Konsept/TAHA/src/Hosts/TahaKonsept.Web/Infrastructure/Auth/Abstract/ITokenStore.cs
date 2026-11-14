using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Web.Infrastructure.Auth.Abstract;

/// <summary>
/// Geçerli isteğin oturumundaki API token'larını okur / yazar.
/// <see cref="GetAsync"/> token'ları döner (oturum yoksa null), <see cref="SaveAsync"/> token'larla oturumu açar ya da
/// yeniler (cookie yeniden yazılır), <see cref="ClearAsync"/> oturumu kapatır.
/// </summary>
public interface ITokenStore
{
    /// <summary>Geçerli oturumun token'larını döner; oturum yoksa null.</summary>
    Task<TokenResponse?> GetAsync();

    /// <summary>Token'ları oturuma yazar (giriş ya da yenileme). Kimlik bilgileri erişim token'ından kurulur.</summary>
    /// <param name="tokens">API'den gelen token'lar.</param>
    /// <param name="isPersistent">Kalıcı cookie mi (null: mevcut oturumun ayarını koru).</param>
    Task SaveAsync(TokenResponse tokens, bool? isPersistent = null);

    /// <summary>Oturumu kapatır (auth cookie silinir).</summary>
    Task ClearAsync();
}
