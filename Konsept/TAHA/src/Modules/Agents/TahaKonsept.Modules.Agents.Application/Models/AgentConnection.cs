namespace TahaKonsept.Modules.Agents.Application.Models;

/// <summary>
/// Hub'a bağlı tek bir ajan bağlantısının bellekteki kaydı (<c>IAgentConnectionTracker</c> tutar).
/// Bağlanınca kimlik bilgileri, <c>Hello</c> gelince ağ bilgileri (<see cref="LanAddress"/>, <see cref="VideoServerPort"/>,
/// <see cref="StreamKey"/> — düz metin yalnızca bellekte) dolar. <see cref="Abort"/> bağlantıyı sunucudan keser.
/// </summary>
public sealed class AgentConnection
{
    /// <summary>Oluşturur.</summary>
    /// <param name="connectionId">SignalR bağlantı kimliği.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="deviceId">Cihaz (bilinmiyorsa <c>null</c>).</param>
    /// <param name="connectedAtUtc">Bağlanma anı.</param>
    /// <param name="abort">Bağlantıyı kesen eylem.</param>
    public AgentConnection(string connectionId, Guid userId, Guid? deviceId, DateTimeOffset connectedAtUtc, Action abort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ConnectionId = connectionId;
        UserId = userId;
        DeviceId = deviceId;
        ConnectedAtUtc = connectedAtUtc;
        LastSeenAtUtc = connectedAtUtc;
        Abort = abort ?? (() => { });
    }

    /// <summary>SignalR bağlantı kimliği.</summary>
    public string ConnectionId { get; }

    /// <summary>Kullanıcı kimliği.</summary>
    public Guid UserId { get; }

    /// <summary>Cihaz kimliği (Hello ile sonradan atanabilir).</summary>
    public Guid? DeviceId { get; set; }

    /// <summary>Bağlanma anı (UTC).</summary>
    public DateTimeOffset ConnectedAtUtc { get; }

    /// <summary>Son sinyal anı (UTC).</summary>
    public DateTimeOffset LastSeenAtUtc { get; set; }

    /// <summary>Son bilinen anın veritabanına yazıldığı an (UTC).</summary>
    public DateTimeOffset? LastPersistedAtUtc { get; set; }

    /// <summary>Hello alındı mı.</summary>
    public bool HelloReceived { get; set; }

    /// <summary>Bilgisayar adı (Hello).</summary>
    public string? MachineName { get; set; }

    /// <summary>LAN adresi (Hello).</summary>
    public string? LanAddress { get; set; }

    /// <summary>Video sunucusu portu (Hello).</summary>
    public int? VideoServerPort { get; set; }

    /// <summary>Akış anahtarı (Hello; yalnızca bellekte düz metin).</summary>
    public string? StreamKey { get; set; }

    /// <summary>Bağlantıyı sunucu tarafında kesen eylem.</summary>
    public Action Abort { get; }
}
