namespace Konsept.Modules.Identity.Contracts.Abstract;

/// <summary>
/// Ajan cihazları iptal edildiğinde (çıkış — BR-14 —, yeni eşleştirme, ileride ban) Identity'nin haber verdiği dinleyici
/// (spec 0012 Req 6, 12). AgentGateway uygular: o cihazların canlı bağlantısına "oturum kapandı" gönderir ve bağlantıyı keser.
/// Identity AgentGateway'e bağımlı olmasın diye arayüz Identity'nin <c>Contracts</c>'ındadır (olay deseni).
/// </summary>
public interface IAgentDeviceRevocationListener
{
    /// <summary>İptal edilen cihazları bildirir.</summary>
    /// <param name="userId">Cihazların sahibi.</param>
    /// <param name="deviceIds">İptal edilen cihazlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task OnDevicesRevokedAsync(Guid userId, IReadOnlyCollection<Guid> deviceIds, CancellationToken cancellationToken);
}
