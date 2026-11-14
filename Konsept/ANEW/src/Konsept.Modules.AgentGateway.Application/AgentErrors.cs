using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AgentGateway.Application;

/// <summary>
/// AgentGateway'in beklenen hataları (kararlı kod + Türkçe mesaj).
/// </summary>
public static class AgentErrors
{
    /// <summary>Gets ajan bağlı değil.</summary>
    public static Error NotConnected { get; } = new("agent.not_connected", "Ajan bağlı değil", ErrorType.Unavailable);

    /// <summary>Gets ajan süresinde cevap vermedi.</summary>
    public static Error Timeout { get; } = new("agent.timeout", "Zaman aşımı", ErrorType.Timeout);
}
