using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Domain.Entities;

/// <summary>Ajanın yerel logundan bir satır (zaman, seviye, maskelenmiş metin).</summary>
/// <param name="At">Zaman.</param>
/// <param name="Level">Seviye.</param>
/// <param name="Text">Metin.</param>
public sealed record AgentLogLine(DateTimeOffset At, AgentLogLevel Level, string Text);
