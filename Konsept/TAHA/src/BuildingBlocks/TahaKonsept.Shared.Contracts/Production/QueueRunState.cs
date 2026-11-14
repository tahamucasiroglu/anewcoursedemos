namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Kullanıcının üretim sırasının genel çalışma durumu (Sırayla üret / Durdur / Devam).
/// </summary>
public enum QueueRunState
{
    /// <summary>Çalışmıyor (başlatılmadı, bitti ya da bütçe nedeniyle durdu).</summary>
    Idle = 0,

    /// <summary>Sıra sırayla işleniyor.</summary>
    Running = 1,

    /// <summary>Durduruldu; mevcut iş kontrol noktasında bekliyor.</summary>
    Paused = 2,
}
