namespace TahaKonsept.Agent.Core.Hub;

/// <summary>Ajanın sunucu hub bağlantı durumu (tepsi ikonu/pencere gösterir).</summary>
public enum HubConnectionStatus
{
    /// <summary>Bağlı değil (durduruldu).</summary>
    Disconnected = 0,

    /// <summary>Bağlanıyor.</summary>
    Connecting = 1,

    /// <summary>Bağlı.</summary>
    Connected = 2,

    /// <summary>Bağlantı koptu, yeniden deneniyor.</summary>
    Reconnecting = 3,

    /// <summary>Yetki yok (eşlenmemiş ya da oturum sunucuda kapatıldı); eşleme bekleniyor.</summary>
    Unauthorized = 4,
}
