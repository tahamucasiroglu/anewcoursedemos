namespace TahaKonsept.Agent.Core.Pairing;

/// <summary>Cihaz eşleme durumu (eşleme penceresi bunu gösterir).</summary>
public enum PairingState
{
    /// <summary>Eşlenmemiş.</summary>
    NotPaired = 0,

    /// <summary>Sunucudan kod isteniyor.</summary>
    Starting = 1,

    /// <summary>Kullanıcının web'de kodu onaylaması bekleniyor.</summary>
    WaitingForUser = 2,

    /// <summary>Eşlendi; token'lar kaydedildi.</summary>
    Paired = 3,

    /// <summary>Kodun süresi doldu; yeniden başlatılmalı.</summary>
    Expired = 4,

    /// <summary>Kullanıcı reddetti.</summary>
    Denied = 5,

    /// <summary>Hata (ağ/sunucu).</summary>
    Failed = 6,

    /// <summary>Kullanıcı vazgeçti.</summary>
    Cancelled = 7,
}
