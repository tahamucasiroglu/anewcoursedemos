namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Ajanın "test et" komutuna cevabı.
/// </summary>
/// <param name="MachineName">Bilgisayar adı.</param>
/// <param name="AgentVersion">Ajan (ürün) versiyonu.</param>
public sealed record AgentPingReply(string MachineName, string AgentVersion);
