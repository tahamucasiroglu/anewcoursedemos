namespace Konsept.Agent.Domain.Enums;

/// <summary>
/// Ajanın oturum durumu (spec 0012 Req 1, 4–6, 9, 11–12, 15): pencere ve tray menüsünün en üst satırı bunu gösterir.
/// </summary>
public enum AgentSessionState
{
    /// <summary>Sunucuya ulaşılamıyor; kendiliğinden yeniden deneniyor.</summary>
    Unreachable,

    /// <summary>Eşleşmedi: kod ve geri sayım gösteriliyor.</summary>
    Unpaired,

    /// <summary>Kodun süresi doldu; kullanıcı yeni kod alabilir.</summary>
    CodeExpired,

    /// <summary>Kullanıcı eşleştirmeyi reddetti; yeni kod alınabilir.</summary>
    Rejected,

    /// <summary>Eşleşti ve sunucuya bağlı.</summary>
    Paired,

    /// <summary>Oturum kapandı (web'den çıkış ya da yeni eşleştirme); yeni kod gösteriliyor.</summary>
    SessionClosed,
}
