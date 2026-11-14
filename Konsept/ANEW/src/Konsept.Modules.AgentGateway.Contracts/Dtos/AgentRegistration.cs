namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Ajanın bağlanınca gönderdiği kimlik bilgisi.
/// </summary>
/// <param name="MachineName">Bilgisayar adı.</param>
/// <param name="AgentVersion">Ajan (ürün) versiyonu.</param>
public sealed record AgentRegistration(string MachineName, string AgentVersion);
