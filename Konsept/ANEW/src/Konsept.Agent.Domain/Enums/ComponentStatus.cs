namespace Konsept.Agent.Domain.Enums;

/// <summary>Bileşen satırının durumu (spec 0009 Req 5, 20).</summary>
public enum ComponentStatus
{
    /// <summary>Bekliyor.</summary>
    Waiting = 0,

    /// <summary>İndiriliyor.</summary>
    Downloading = 1,

    /// <summary>Doğrulanıyor.</summary>
    Verifying = 2,

    /// <summary>Kuruluyor.</summary>
    Installing = 3,

    /// <summary>Kuruldu.</summary>
    Installed = 4,

    /// <summary>Atlandı (seçilmeyen isteğe bağlı bileşen).</summary>
    Skipped = 5,

    /// <summary>Hata.</summary>
    Failed = 6,

    /// <summary>Onarılmalı (bütünlük denetimi).</summary>
    NeedsRepair = 7,
}
