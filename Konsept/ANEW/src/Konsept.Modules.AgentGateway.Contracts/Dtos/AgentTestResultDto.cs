namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// "Ajanı test et" sonucunda ekranda gösterilen bilgiler.
/// </summary>
/// <param name="MachineName">Ajanın bilgisayar adı.</param>
/// <param name="AgentVersion">Ajanın versiyonu.</param>
public sealed record AgentTestResultDto(string MachineName, string AgentVersion);
