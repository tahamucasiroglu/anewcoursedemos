using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// Ajanın bağlanma/kopma olaylarını işler: oturumu sahibiyle kaydeder ve olayı sahibine bağlı olarak genel olay kaydına yazar
/// (spec 0012 Req 13).
/// </summary>
public interface IAgentPresenceService : IAgentStatusReader
{
    /// <summary>Ajanın kaydını işler.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <param name="owner">Hub kimliğinden gelen sahip ve cihaz.</param>
    /// <param name="registration">Ajanın bilgileri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sunucu sürümü ve güncelleme gerekip gerekmediği (spec 0023 Req 4).</returns>
    Task<AgentRegisterReply> RegisterAsync(string connectionId, AgentOwner owner, AgentRegistration registration, CancellationToken cancellationToken);

    /// <summary>
    /// Ajan güncellemeye başlamak istiyor (spec 0023 Req 3): sahibin "Üretiliyor" işi varsa reddedilir; değilse oturum "güncelleniyor" olur
    /// ve güncelleme bitene (ajan yeniden bağlanana) kadar ajana komut gitmez.
    /// </summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <param name="owner">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Başlayabilirse <c>true</c>.</returns>
    Task<bool> BeginUpdateAsync(string connectionId, AgentOwner owner, CancellationToken cancellationToken);

    /// <summary>
    /// Güncelleme kurulmadan bitti (doğrulama/indirme/disk hatası; review r1 H-1): oturum yeniden komut alır, bekleyen iş sürer.
    /// </summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <param name="owner">Sahip.</param>
    void EndUpdate(string connectionId, AgentOwner owner);

    /// <summary>
    /// Ajanın kurulu bileşen listesi değişti (spec 0027 Req 9, B-149): yalnızca sahibinin kayıtlı geçerli oturumundan gelirse saklanır.
    /// </summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="owner">Oturumun sahibi.</param>
    /// <param name="components">Kurulu bileşen kimlikleri.</param>
    /// <returns>Kabul edildiyse <c>true</c>.</returns>
    bool ReportComponents(string connectionId, AgentOwner owner, IReadOnlyList<string> components);

    /// <summary>Bağlantının kopmasını işler.</summary>
    /// <param name="connectionId">Kopan bağlantının kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task DisconnectAsync(string connectionId, CancellationToken cancellationToken);
}
