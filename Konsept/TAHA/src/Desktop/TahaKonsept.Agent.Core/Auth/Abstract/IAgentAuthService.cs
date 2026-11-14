using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Agent.Core.Auth.Abstract;

/// <summary>
/// Ajanın oturum durumu: token'ları tutar, süresi dolmak üzere olan erişim token'ını yeniler (tek uçuş),
/// yenileme reddedilirse (ör. web'den çıkış → tüm token'lar iptal) oturumu kapatır ve <see cref="AuthorizationChanged"/> tetikler.
/// SignalR <c>AccessTokenProvider</c> ve <c>AgentApiClient</c> bunu kullanır.
/// </summary>
public interface IAgentAuthService
{
    /// <summary>Ajan eşlenmiş mi (geçerli yenileme token'ı var mı).</summary>
    bool IsPaired { get; }

    /// <summary>Geçerli erişim token'ını döner; gerekirse önce yeniler. Eşlenmemişse <c>null</c>.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Token ya da <c>null</c>.</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken ct);

    /// <summary>Token'ı hemen yeniler (ör. 401 sonrası).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yenilendiyse <c>true</c>.</returns>
    Task<bool> RefreshAsync(CancellationToken ct);

    /// <summary>Eşleme sonucu gelen token'ları kaydeder.</summary>
    /// <param name="tokens">Token çifti.</param>
    void SetTokens(TokenResponse tokens);

    /// <summary>Oturumu kapatır (token'lar silinir).</summary>
    void SignOut();

    /// <summary>Eşlenme durumu değişince tetiklenir.</summary>
    event EventHandler? AuthorizationChanged;
}
