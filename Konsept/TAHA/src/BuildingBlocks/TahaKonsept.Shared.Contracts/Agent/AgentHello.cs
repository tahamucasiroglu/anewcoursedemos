namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajan hub'a bağlanınca gönderdiği tanıtım: sürüm, makine adı, LAN IP, yerel video sunucu portu ve akış anahtarı,
/// donanım/kurulu bileşenler. Web, video oynatıcı adresini bu bilgilerle kurar.
/// </summary>
public sealed record AgentHello
{
    /// <summary>Ajan sürümü.</summary>
    public string AgentVersion { get; init; } = string.Empty;

    /// <summary>Bilgisayar adı.</summary>
    public string MachineName { get; init; } = string.Empty;

    /// <summary>İşletim sistemi açıklaması.</summary>
    public string OsDescription { get; init; } = string.Empty;

    /// <summary>Yerel ağ IP adresi.</summary>
    public string LanIpAddress { get; init; } = string.Empty;

    /// <summary>Yerel video sunucusu portu (varsayılan 7120).</summary>
    public int VideoServerPort { get; init; } = 7120;

    /// <summary>Video sunucusu akış anahtarı (<c>?k=</c>).</summary>
    public string StreamKey { get; init; } = string.Empty;

    /// <summary>Donanım ve kurulu bileşenler.</summary>
    public AgentCapabilities Capabilities { get; init; } = new();
}
