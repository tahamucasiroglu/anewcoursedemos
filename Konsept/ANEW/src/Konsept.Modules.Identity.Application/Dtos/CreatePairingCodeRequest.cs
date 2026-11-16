namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Eşleştirilmemiş ajanın kod isteği (spec 0012 Req 1, 3).</summary>
/// <param name="MachineName">Makine adı.</param>
/// <param name="AgentVersion">Ajan sürümü.</param>
public sealed record CreatePairingCodeRequest(string? MachineName, string? AgentVersion);
