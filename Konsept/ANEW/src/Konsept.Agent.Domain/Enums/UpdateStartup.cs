namespace Konsept.Agent.Domain.Enums;

/// <summary>Açılıştaki güncelleme denetiminin sonucu (spec 0023 Req 9).</summary>
public enum UpdateStartup
{
    /// <summary>Bekleyen güncelleme yok.</summary>
    Normal = 0,

    /// <summary>Yeni sürüm ilk kez açıldı; 60 sn içinde bağlanması bekleniyor.</summary>
    Watching = 1,

    /// <summary>Önceki sürüme dönüldü ve bu (önceki) sürüm açıldı: "Yeni sürüm başlatılamadı, önceki sürüme dönüldü".</summary>
    RolledBack = 2,

    /// <summary>Yarım kalan yeni sürüm geri alındı ve önceki sürüm başlatıldı; bu süreç kapanmalı.</summary>
    Restarting = 3,
}
