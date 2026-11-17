namespace Konsept.Modules.AgentGateway.Domain.Entities;

/// <summary>
/// Bağlı bir ajanın oturumu: SignalR bağlantı kimliği, ajanın kendini tanıttığı bilgiler ve sahibi (spec 0012 Req 13).
/// </summary>
/// <param name="ConnectionId">SignalR bağlantı kimliği.</param>
/// <param name="MachineName">Bilgisayar adı.</param>
/// <param name="AgentVersion">Ajan versiyonu.</param>
/// <param name="ConnectedAt">Kayıt zamanı (UTC).</param>
/// <param name="UserId">Sahip (ajanı eşleştiren kullanıcı).</param>
/// <param name="DeviceId">Eşleşmiş cihaz.</param>
/// <param name="UpdateRequired">Ajanın sürümü sunucununkinden farklı (spec 0023 Req 4, 15): ona hiçbir komut gönderilmez.</param>
/// <param name="Updating">Ajan güncellemeye başladı (spec 0023 Req 3): güncelleme bitene kadar ona iş gönderilmez.</param>
public sealed record AgentSession(
    string ConnectionId,
    string MachineName,
    string AgentVersion,
    DateTimeOffset ConnectedAt,
    Guid UserId = default,
    Guid DeviceId = default,
    bool UpdateRequired = false,
    bool Updating = false)
{
    /// <summary>Gets a value indicating whether ajana komut gönderilebilir (sürüm eşleşiyor ve güncellemede değil).</summary>
    public bool AcceptsCommands => !UpdateRequired && !Updating;
}
