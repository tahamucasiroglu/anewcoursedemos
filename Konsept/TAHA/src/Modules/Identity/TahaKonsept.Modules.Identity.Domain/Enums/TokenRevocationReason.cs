namespace TahaKonsept.Modules.Identity.Domain.Enums;

/// <summary>
/// Refresh token'ın neden iptal edildiği (denetim için DB'de metin olarak saklanır).
/// </summary>
public enum TokenRevocationReason
{
    /// <summary>Yenileme sırasında yeni token ile değiştirildi (normal rotasyon).</summary>
    Rotated = 0,

    /// <summary>Kullanılmış token tekrar geldi: aile ele geçirilmiş sayıldı.</summary>
    ReuseDetected = 1,

    /// <summary>Kullanıcı tüm oturumlardan çıktı.</summary>
    LogoutAll = 2,

    /// <summary>Kullanıcı yasaklandı.</summary>
    UserBanned = 3,

    /// <summary>Aile tek başına iptal edildi (ör. ajan kaldırıldı).</summary>
    FamilyRevoked = 4,
}
