namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Ajanın gördüğü kod durumları (protokol değerleri).</summary>
public static class PairingStatusValues
{
    /// <summary>Karar bekleniyor.</summary>
    public const string Pending = "pending";

    /// <summary>Onaylandı (belirteç bu yanıtta).</summary>
    public const string Approved = "approved";

    /// <summary>Reddedildi.</summary>
    public const string Rejected = "rejected";

    /// <summary>Süresi doldu, kullanıldı ya da bulunamadı: ajan yeni kod almalı.</summary>
    public const string Expired = "expired";
}
