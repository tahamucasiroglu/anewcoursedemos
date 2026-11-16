namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// Ajan oturumu ve eşleştirme kodu süreleri (spec 0012 Req 1, 9): ajan oturumu son kullanımdan itibaren 90 gün geçerlidir ve
/// kullanıldıkça uzar (son kullanım en fazla dakikada bir yazılır); eşleştirme kodu 10 dakika geçerlidir.
/// </summary>
public static class AgentSessionPolicy
{
    /// <summary>Kayan ajan oturumu süresi.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);

    /// <summary>Eşleştirme kodunun ömrü.</summary>
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    /// <summary>Son kullanımdan bu yana süre doldu mu.</summary>
    /// <param name="lastUsedAt">Son kullanım.</param>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Dolduysa <c>true</c>.</returns>
    public static bool IsExpired(DateTimeOffset lastUsedAt, DateTimeOffset now) => now - lastUsedAt > Lifetime;
}
