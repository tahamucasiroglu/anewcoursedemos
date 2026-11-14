namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// Oturum süresi kuralı (spec 0002 Req 13): oturum son kullanımdan itibaren 14 gün geçerlidir ve kullanıldıkça uzar.
/// Son kullanım zamanı en fazla dakikada bir güncellenir (her istekte yazma olmasın, plan Risk 6).
/// </summary>
public static class SessionPolicy
{
    /// <summary>Kayan oturum süresi.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);

    /// <summary>Son kullanımın yeniden yazılması için gereken en kısa ara.</summary>
    public static readonly TimeSpan TouchThreshold = TimeSpan.FromMinutes(1);

    /// <summary>Son kullanımdan bu yana süre doldu mu.</summary>
    /// <param name="lastUsedAt">Son kullanım.</param>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Dolduysa <c>true</c>.</returns>
    public static bool IsExpired(DateTimeOffset lastUsedAt, DateTimeOffset now) => now - lastUsedAt > Lifetime;

    /// <summary>Son kullanım zamanı güncellenmeli mi.</summary>
    /// <param name="lastUsedAt">Son kullanım.</param>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Güncellenmeliyse <c>true</c>.</returns>
    public static bool ShouldTouch(DateTimeOffset lastUsedAt, DateTimeOffset now) => now - lastUsedAt >= TouchThreshold;
}
