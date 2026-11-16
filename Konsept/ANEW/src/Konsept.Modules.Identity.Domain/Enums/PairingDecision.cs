namespace Konsept.Modules.Identity.Domain.Enums;

/// <summary>
/// Bir eşleştirme koduna onay/ret kararının sonucu (spec 0012 Req 5).
/// </summary>
public enum PairingDecision
{
    /// <summary>Karar uygulandı.</summary>
    Accepted,

    /// <summary>Kodun süresi dolmuş.</summary>
    Expired,

    /// <summary>Kod daha önce onaylanmış ya da reddedilmiş (tek kullanımlık).</summary>
    AlreadyUsed,
}
