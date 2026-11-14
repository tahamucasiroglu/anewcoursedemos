namespace TahaKonsept.Agent.Core.Execution;

/// <summary>Bir işin neden durdurulduğu.</summary>
public enum JobStopReason
{
    /// <summary>Durdurulmadı.</summary>
    None = 0,

    /// <summary>Duraklatıldı: mevcut adım iptal edilir, kontrol noktası dosyaları korunur; devamda yalnız eksik çıktılar üretilir.</summary>
    Paused = 1,

    /// <summary>İptal edildi: iş çalışma alanı silinir.</summary>
    Cancelled = 2,

    /// <summary>Ajan kapanıyor: dosyalar korunur, sunucu yeniden gönderebilir.</summary>
    Shutdown = 3,
}
