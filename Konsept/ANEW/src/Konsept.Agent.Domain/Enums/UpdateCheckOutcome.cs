namespace Konsept.Agent.Domain.Enums;

/// <summary>Güncelleme denetiminin sonucu (spec 0023 Req 1, 7, 8).</summary>
public enum UpdateCheckOutcome
{
    /// <summary>Ajan güncel (yeni sürüm yok ya da liste eski/aynı sürümü taşıyor).</summary>
    UpToDate = 0,

    /// <summary>Yeni sürüm hazır.</summary>
    Available = 1,

    /// <summary>İmza ya da liste doğrulanamadı; hiçbir şey kurulmaz.</summary>
    VerificationFailed = 2,

    /// <summary>Sunucuya ulaşılamadı.</summary>
    Unavailable = 3,

    /// <summary>Bu derlemede güncelleme anahtarı yok (geliştirme derlemesi); denetim yapılmaz.</summary>
    Disabled = 4,
}
