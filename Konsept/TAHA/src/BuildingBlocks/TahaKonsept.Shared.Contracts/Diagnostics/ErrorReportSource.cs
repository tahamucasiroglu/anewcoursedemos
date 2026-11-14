namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Hata bildiriminin geldiği yer.
/// </summary>
public enum ErrorReportSource
{
    /// <summary>Web arayüzü ("Hata bildir" penceresi).</summary>
    Web = 0,

    /// <summary>Masaüstü ajanı (tepsi uygulaması).</summary>
    Agent = 1,

    /// <summary>Masaüstü kurulum / güncelleme ekranı.</summary>
    Installer = 2,

    /// <summary>API sunucusunun kendisi.</summary>
    Api = 3,
}
