namespace Konsept.Modules.AgentGateway.Domain.Entities;

/// <summary>
/// Bağlı bir ajanın oturumu: SignalR bağlantı kimliği ve ajanın kendini tanıttığı bilgiler.
/// </summary>
/// <param name="ConnectionId">SignalR bağlantı kimliği.</param>
/// <param name="MachineName">Bilgisayar adı.</param>
/// <param name="AgentVersion">Ajan versiyonu.</param>
/// <param name="ConnectedAt">Kayıt zamanı (UTC).</param>
public sealed record AgentSession(string ConnectionId, string MachineName, string AgentVersion, DateTimeOffset ConnectedAt);
