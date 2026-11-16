namespace Konsept.Modules.Identity.Domain.Enums;

/// <summary>
/// Eşleştirme isteğinin durumu (spec 0012 Req 3–5). Kod yalnızca <see cref="Pending"/> iken onaylanabilir ya da reddedilebilir.
/// </summary>
public enum PairingState
{
    /// <summary>Kod gösteriliyor, karar bekleniyor.</summary>
    Pending,

    /// <summary>Onaylandı; cihaz kaydediliyor (belirteç henüz hazır değil).</summary>
    Approving,

    /// <summary>Onaylandı; belirteç ajana teslim edilmeyi bekliyor.</summary>
    Approved,

    /// <summary>Belirteç ajana teslim edildi (kod tükendi).</summary>
    Delivered,

    /// <summary>Kullanıcı reddetti.</summary>
    Rejected,
}
