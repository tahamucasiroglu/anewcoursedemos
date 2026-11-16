namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Kod durumu (<see cref="PairingStatusValues"/>); onaylandıysa ajan oturum belirteci (yalnızca bir kez).</summary>
/// <param name="State">Durum.</param>
/// <param name="AgentToken">Ajan oturum belirteci; yalnızca <c>approved</c>'da.</param>
public sealed record PairingStatusDto(string State, string? AgentToken = null);
