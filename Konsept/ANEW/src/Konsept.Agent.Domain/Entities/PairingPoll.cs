using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Domain.Entities;

/// <summary>Kod durumu (spec 0012 Req 4–5); onaylandıysa ajan oturum belirteci.</summary>
/// <param name="State">Durum.</param>
/// <param name="Token">Belirteç; yalnızca onaylandıysa.</param>
public sealed record PairingPoll(PairingPollState State, string? Token = null);
