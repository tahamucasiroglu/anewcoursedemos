namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Hata bildiriminin yönetim panelindeki durumu. Akış: <see cref="New"/> → <see cref="Seen"/> → <see cref="Resolved"/>;
/// <see cref="New"/> doğrudan <see cref="Resolved"/> olabilir, çözülmüş bildirim yeniden <see cref="New"/> yapılabilir (yeniden aç).
/// </summary>
public enum ErrorReportStatus
{
    /// <summary>Yeni gelmiş, henüz bakılmamış.</summary>
    New = 0,

    /// <summary>Yönetici gördü, inceleniyor.</summary>
    Seen = 1,

    /// <summary>Çözüldü.</summary>
    Resolved = 2,
}
