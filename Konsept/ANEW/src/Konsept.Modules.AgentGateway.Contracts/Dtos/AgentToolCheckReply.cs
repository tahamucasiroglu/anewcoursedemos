namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Ajanın araç denetimi cevabı: kurulu olmayan araçların kimlikleri (boşsa hepsi hazır).</summary>
/// <param name="Missing">Eksik araçlar.</param>
public sealed record AgentToolCheckReply(IReadOnlyList<string> Missing);
