namespace TahaKonsept.Agent.ViewModels;

/// <summary>Kurulum penceresindeki bir ön gereksinim adımının durumu.</summary>
public enum SetupStepState
{
    /// <summary>Kurulu değil, sırada.</summary>
    Pending = 0,

    /// <summary>Zaten kurulu.</summary>
    Installed = 1,

    /// <summary>Kuruluyor.</summary>
    Running = 2,

    /// <summary>Bu oturumda kuruldu.</summary>
    Done = 3,

    /// <summary>Kurulamadı.</summary>
    Failed = 4,

    /// <summary>İsteğe bağlı, seçilmedi.</summary>
    Skipped = 5,
}
