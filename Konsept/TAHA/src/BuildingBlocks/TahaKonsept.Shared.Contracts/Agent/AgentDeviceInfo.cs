namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Eşlenmiş masaüstü programının özeti (<c>GET api/agents</c> listesi ve <c>IAgentGateway.GetOnlineAgentAsync</c> sonucu).
/// Önemli alanlar: <see cref="IsOnline"/> (şu an hub'a bağlı mı), <see cref="LastSeenAtUtc"/>, <see cref="LanAddress"/>/<see cref="VideoServerPort"/>.
/// Akış anahtarı güvenlik nedeniyle burada yer almaz.
/// </summary>
public sealed record AgentDeviceInfo
{
    /// <summary>Cihaz kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Bilgisayar adı.</summary>
    public string MachineName { get; init; } = string.Empty;

    /// <summary>Ajan sürümü.</summary>
    public string AgentVersion { get; init; } = string.Empty;

    /// <summary>İşletim sistemi açıklaması.</summary>
    public string? OsDescription { get; init; }

    /// <summary>Eşleme zamanı (UTC).</summary>
    public DateTimeOffset PairedAtUtc { get; init; }

    /// <summary>Son görülme zamanı (UTC; hiç bağlanmadıysa <c>null</c>).</summary>
    public DateTimeOffset? LastSeenAtUtc { get; init; }

    /// <summary>Şu an çevrimiçi mi.</summary>
    public bool IsOnline { get; init; }

    /// <summary>Yerel ağ adresi.</summary>
    public string? LanAddress { get; init; }

    /// <summary>Yerel video sunucusu portu.</summary>
    public int? VideoServerPort { get; init; }

    /// <summary>Birincil GPU adı (varsa).</summary>
    public string? GpuName { get; init; }

    /// <summary>Birincil GPU VRAM (MB).</summary>
    public long? VramMb { get; init; }

    /// <summary>Son bildirilen donanım/kurulu bileşenler.</summary>
    public AgentCapabilities? Capabilities { get; init; }
}
