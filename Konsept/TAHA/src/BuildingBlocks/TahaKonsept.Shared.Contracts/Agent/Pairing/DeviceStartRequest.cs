namespace TahaKonsept.Shared.Contracts.Agent.Pairing;

/// <summary>
/// Ajanın cihaz eşlemeyi başlatma isteği (<c>POST api/agents/device/start</c>).
/// </summary>
public sealed record DeviceStartRequest
{
    /// <summary>Bilgisayar adı.</summary>
    public string MachineName { get; init; } = string.Empty;

    /// <summary>Ajan sürümü.</summary>
    public string AgentVersion { get; init; } = string.Empty;
}
