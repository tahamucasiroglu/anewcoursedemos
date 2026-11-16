namespace Konsept.Modules.AiGateway.Domain.Rules;

/// <summary>
/// Yeniden deneme kuralı (spec 0013 Req 7, H-3, docs/security.md): yalnızca 429, 5xx ya da hiç kurulamayan bağlantıda; en fazla 2
/// tekrar (toplam 3 deneme); bekleme her tekrarda artar (1 sn, 2 sn). Diğer 4xx ve zaman aşımı asla tekrar edilmez.
/// </summary>
public static class RetryRule
{
    /// <summary>Toplam deneme üst sınırı.</summary>
    public const int MaxAttempts = 3;

    /// <summary>HTTP durum kodu tekrar gerektirir mi.</summary>
    /// <param name="statusCode">Durum kodu.</param>
    /// <returns>429 ya da 5xx ise <c>true</c>.</returns>
    public static bool IsRetryableStatus(int statusCode) => statusCode == 429 || statusCode is >= 500 and <= 599;

    /// <summary><paramref name="failedAttempt"/>. denemeden sonraki bekleme.</summary>
    /// <param name="failedAttempt">Başarısız denemenin sırası (1 tabanlı).</param>
    /// <returns>Bekleme (1 → 1 sn, 2 → 2 sn).</returns>
    public static TimeSpan DelayAfter(int failedAttempt) => TimeSpan.FromSeconds(1 << Math.Clamp(failedAttempt - 1, 0, 10));
}
