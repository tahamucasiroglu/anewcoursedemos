namespace Konsept.Agent.Domain.Enums;

/// <summary>Kod durumu sorgusunun sonucu (spec 0012 Req 4–5).</summary>
public enum PairingPollState
{
    /// <summary>Karar bekleniyor.</summary>
    Pending,

    /// <summary>Onaylandı; belirteç geldi.</summary>
    Approved,

    /// <summary>Reddedildi.</summary>
    Rejected,

    /// <summary>Süresi doldu, kullanıldı ya da sunucu kodu tanımıyor.</summary>
    Expired,
}
