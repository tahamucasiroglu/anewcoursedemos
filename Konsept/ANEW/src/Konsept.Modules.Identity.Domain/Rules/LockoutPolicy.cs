namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// Hesap kilidi kuralı (spec 0002 Req 6–7): 3 yanlış denemede hesap 15 dakika kilitlenir; kilitliyken yapılan denemeler
/// süreyi uzatmaz ve sayacı değiştirmez; kilitlenince sayaç sıfırlanır, böylece süre dolunca yeniden 3 hak başlar.
/// Saf kural: veritabanı aynı kararı tek atomik SQL ifadesiyle uygular (plan Risk 3), test sahteleri bunu çağırır.
/// </summary>
public static class LockoutPolicy
{
    /// <summary>Kilitten önce izin verilen yanlış deneme sayısı.</summary>
    public const int MaxFailedAttempts = 3;

    /// <summary>Kilit süresi.</summary>
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    /// <summary>Hesap şu an kilitli mi.</summary>
    /// <param name="lockedUntil">Kilidin açılma zamanı.</param>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Kilitliyse <c>true</c>.</returns>
    public static bool IsLocked(DateTimeOffset? lockedUntil, DateTimeOffset now) =>
        lockedUntil is { } until && until > now;

    /// <summary>Yanlış bir şifre denemesinin sonucunu hesaplar.</summary>
    /// <param name="failedCount">Denemeden önceki yanlış deneme sayısı.</param>
    /// <param name="lockedUntil">Denemeden önceki kilit zamanı.</param>
    /// <param name="now">Deneme zamanı.</param>
    /// <returns>Yeni durum; kilitliyken <see cref="LockoutOutcome.Applied"/> <c>false</c> ve durum değişmez.</returns>
    public static LockoutOutcome Evaluate(int failedCount, DateTimeOffset? lockedUntil, DateTimeOffset now)
    {
        if (IsLocked(lockedUntil, now))
        {
            return new LockoutOutcome(false, failedCount, lockedUntil);
        }

        var next = failedCount + 1;
        return next >= MaxFailedAttempts
            ? new LockoutOutcome(true, 0, now + LockDuration)
            : new LockoutOutcome(true, next, lockedUntil);
    }

    /// <summary>Yanlış deneme sonrasında kalan hak.</summary>
    /// <param name="failedCount">Yanlış deneme sayısı.</param>
    /// <returns>Kalan hak (0'dan küçük olmaz).</returns>
    public static int RemainingAttempts(int failedCount) => Math.Max(0, MaxFailedAttempts - failedCount);
}
