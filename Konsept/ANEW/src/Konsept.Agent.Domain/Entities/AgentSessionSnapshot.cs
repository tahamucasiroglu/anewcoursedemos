using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Domain.Entities;

/// <summary>Ajan penceresinin ve tray satırının gösterdiği anlık oturum durumu (spec 0012 Req 1, 15).</summary>
/// <param name="State">Durum.</param>
/// <param name="Code">Gösterilen kod; yoksa <c>null</c>.</param>
/// <param name="ExpiresAt">Kodun son geçerliliği; yoksa <c>null</c>.</param>
public sealed record AgentSessionSnapshot(AgentSessionState State, string? Code = null, DateTimeOffset? ExpiresAt = null);
