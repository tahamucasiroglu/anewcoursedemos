namespace Konsept.Agent.Domain.Enums;

/// <summary>Ajan log satırının seviyesi (hata bildirimine yalnızca Warning ve üstü eklenir, spec 0022 Req 4).</summary>
public enum AgentLogLevel
{
    /// <summary>Bilgi.</summary>
    Information = 0,

    /// <summary>Uyarı.</summary>
    Warning = 1,

    /// <summary>Hata.</summary>
    Error = 2,

    /// <summary>Kritik.</summary>
    Critical = 3,
}
