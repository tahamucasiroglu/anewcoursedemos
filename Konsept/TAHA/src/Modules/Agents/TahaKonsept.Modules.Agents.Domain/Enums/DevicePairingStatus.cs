namespace TahaKonsept.Modules.Agents.Domain.Enums;

/// <summary>
/// Cihaz eşleme isteğinin durumu: <see cref="Pending"/> → (<see cref="Approved"/> → <see cref="Consumed"/>) | <see cref="Denied"/>.
/// Süre dolması ayrı durum değildir; <c>ExpiresAtUtc</c> ile hesaplanır.
/// </summary>
public enum DevicePairingStatus
{
    /// <summary>Kullanıcı onayı bekleniyor.</summary>
    Pending = 0,

    /// <summary>Kullanıcı onayladı; ajan token'ı henüz almadı.</summary>
    Approved = 1,

    /// <summary>Token verildi (tek seferlik); istek kapandı.</summary>
    Consumed = 2,

    /// <summary>Kullanıcı reddetti.</summary>
    Denied = 3,
}
