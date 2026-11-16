namespace Konsept.Agent.Domain.Enums;

/// <summary>Bileşen hatasının sade sebebi (spec 0009 Req 13, 17).</summary>
public enum SetupFailure
{
    /// <summary>Hata yok.</summary>
    None = 0,

    /// <summary>İnternet bağlantısı koptu.</summary>
    NoInternet = 1,

    /// <summary>İndirme başarısız (kaynağa ulaşılamadı).</summary>
    DownloadFailed = 2,

    /// <summary>Dosya doğrulanamadı (SHA-256 uyuşmadı).</summary>
    VerifyFailed = 3,

    /// <summary>Diskte yeterli yer yok.</summary>
    DiskFull = 4,

    /// <summary>Kurulum (arşiv açma) başarısız.</summary>
    InstallFailed = 5,

    /// <summary>Hazır denetimi başarısız: "Ajan kurulumu eksik: &lt;araç&gt;".</summary>
    ReadyCheckFailed = 6,
}
