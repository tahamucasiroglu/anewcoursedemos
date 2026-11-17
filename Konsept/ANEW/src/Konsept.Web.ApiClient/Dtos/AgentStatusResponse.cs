namespace Konsept.Web.ApiClient.Dtos;

/// <summary>
/// API'nin bildirdiği ajan durumu.
/// </summary>
/// <param name="IsConnected">Ajan bağlı mı.</param>
/// <param name="MachineName">Bilgisayar adı.</param>
/// <param name="AgentVersion">Ajan versiyonu.</param>
/// <param name="ConnectedAt">Bağlanma zamanı (UTC).</param>
/// <param name="UpdateRequired">Ajan bağlı ama sürümü sunucudan farklı: "Ajan güncellemesi gerekli" (spec 0023 Req 4).</param>
public sealed record AgentStatusResponse(bool IsConnected, string? MachineName, string? AgentVersion, DateTimeOffset? ConnectedAt, bool UpdateRequired = false);
