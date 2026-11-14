namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Ajan oturumlarını iptal eder: cihaz(lar)ı iptal edilmiş işaretler ve hub bağlantılarını keser.
/// Identity'nin <c>ISessionRevocationNotifier</c> adaptörü (<c>AgentSessionRevocationNotifier</c>) logout-all'da <see cref="RevokeUserAsync"/>,
/// tek aile iptalinde <see cref="RevokeTokenFamilyAsync"/> çağırır.
/// </summary>
public interface IAgentSessionRevoker
{
    /// <summary>Kullanıcının tüm ajanlarını iptal eder ve bağlantılarını keser.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İptal edilen cihaz sayısı.</returns>
    Task<int> RevokeUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Bu refresh token ailesine bağlı cihazı iptal eder ve bağlantısını keser.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenFamilyId">Aile.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz bulunup iptal edildiyse <c>true</c>.</returns>
    Task<bool> RevokeTokenFamilyAsync(Guid userId, Guid tokenFamilyId, CancellationToken cancellationToken = default);

    /// <summary>Tek cihazın bağlantılarını keser (cihaz kaydı çağıran tarafından iptal edilir).</summary>
    /// <param name="deviceId">Cihaz.</param>
    void DisconnectDevice(Guid deviceId);
}
