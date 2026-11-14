namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Tek bir aşamanın (ya da sahne bazlı aşamanın) durumu.
/// </summary>
public enum StageStatus
{
    /// <summary>Henüz başlamadı.</summary>
    Pending = 0,

    /// <summary>Çalışıyor.</summary>
    Running = 1,

    /// <summary>Tamamlandı.</summary>
    Completed = 2,

    /// <summary>Girdi hash'i değişmediği için atlandı (artefakt yeniden kullanıldı).</summary>
    Skipped = 3,

    /// <summary>Hata ile bitti.</summary>
    Failed = 4,

    /// <summary>İptal edildi.</summary>
    Cancelled = 5,
}
