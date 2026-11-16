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
public sealed record AgentSession(string ConnectionId, string MachineName, string AgentVersion, DateTimeOffset ConnectedAt, Guid UserId = default, Guid DeviceId = default);
