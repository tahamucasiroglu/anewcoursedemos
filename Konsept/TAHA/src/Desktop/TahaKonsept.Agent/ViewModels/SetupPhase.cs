namespace TahaKonsept.Agent.ViewModels;

/// <summary>Kurulum penceresinin aşaması (durum makinesi).</summary>
public enum SetupPhase
{
    /// <summary>Bileşen listesi gösteriliyor, kullanıcı "Kur ve devam et" bekliyor.</summary>
    Prerequisites = 0,

    /// <summary>Bileşenler kuruluyor.</summary>
    Installing = 1,

    /// <summary>Zorunlu bir bileşen kurulamadı; tekrar denenebilir.</summary>
    Failed = 2,

    /// <summary>Cihaz eşleme adımı.</summary>
    Pairing = 3,

    /// <summary>Kurulum bitti.</summary>
    Completed = 4,
}
