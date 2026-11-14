namespace TahaKonsept.Shared.Contracts.Agent.Pairing;

/// <summary>
/// Kod onay yanıtı: onaylanan bilgisayarın adı ve sürümü (web'de bilgi mesajı için).
/// </summary>
public sealed record DeviceApproveResponse
{
    /// <summary>Bilgisayar adı.</summary>
    public string MachineName { get; init; } = string.Empty;

    /// <summary>Ajan sürümü.</summary>
    public string AgentVersion { get; init; } = string.Empty;
}
