namespace TahaKonsept.Modules.Agents.Domain.Enums;

/// <summary>
/// Ajanın token yoklamasının sonucu (RFC 8628 §3.5 karşılıkları).
/// </summary>
public enum PairingPollOutcome
{
    /// <summary>Onay bekleniyor (<c>authorization_pending</c>).</summary>
    Pending = 0,

    /// <summary>Çok sık yoklama (<c>slow_down</c>); aralık artırıldı.</summary>
    SlowDown = 1,

    /// <summary>Kodun süresi doldu ya da kod zaten kullanıldı (<c>expired_token</c>).</summary>
    Expired = 2,

    /// <summary>Kullanıcı reddetti (<c>access_denied</c>).</summary>
    Denied = 3,

    /// <summary>Onaylandı; token verilebilir.</summary>
    Approved = 4,
}
