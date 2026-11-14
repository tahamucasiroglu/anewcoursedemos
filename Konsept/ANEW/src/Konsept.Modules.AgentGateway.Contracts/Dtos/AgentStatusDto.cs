namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Ajanın anlık bağlantı durumu.
/// </summary>
/// <param name="IsConnected">Ajan bağlı mı.</param>
/// <param name="MachineName">Bağlı ajanın bilgisayar adı.</param>
/// <param name="AgentVersion">Bağlı ajanın versiyonu.</param>
/// <param name="ConnectedAt">Bağlanma zamanı (UTC).</param>
public sealed record AgentStatusDto(bool IsConnected, string? MachineName, string? AgentVersion, DateTimeOffset? ConnectedAt);
