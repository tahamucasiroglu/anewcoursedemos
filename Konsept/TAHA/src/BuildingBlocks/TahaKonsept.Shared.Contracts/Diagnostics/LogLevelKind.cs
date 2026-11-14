namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Kullanıcıya görünen günlük seviyesi (alt Günlük şeridi renkleri: Error/Critical kırmızı, Warning sarı, diğerleri nötr).
/// </summary>
public enum LogLevelKind
{
    /// <summary>Ayrıntılı geliştirici bilgisi.</summary>
    Debug = 0,

    /// <summary>Normal bilgi.</summary>
    Information = 1,

    /// <summary>Uyarı.</summary>
    Warning = 2,

    /// <summary>Hata.</summary>
    Error = 3,

    /// <summary>Kritik hata (uygulama/iş devam edemez).</summary>
    Critical = 4,
}
