namespace Konsept.Agent.Domain.Enums;

/// <summary>
/// Ajanın sunucuya (API) bağlantı durumu.
/// </summary>
public enum ConnectionState
{
    /// <summary>Bağlı değil.</summary>
    Disconnected,

    /// <summary>Bağlanıyor ya da yeniden bağlanıyor.</summary>
    Connecting,

    /// <summary>Bağlı ve kayıtlı.</summary>
    Connected,
}
