namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>
/// Süre uyumu (spec 0014 Req 11, G-13): sahne süreleri toplamı hedefin ±tolerans aralığı dışındaysa "süre uyumsuz". Sınırlar dahildir:
/// hedef 60 sn, tolerans 0,15 → 51–69 sn uyumlu; 50 ve 70 sn uyumsuz.
/// </summary>
public static class DurationFit
{
    /// <summary>Toplam süre hedefle uyumsuz mu.</summary>
    /// <param name="totalSeconds">Sahne süreleri toplamı.</param>
    /// <param name="targetSeconds">Hedef süre.</param>
    /// <param name="tolerance">Tolerans (0,15).</param>
    /// <returns>Uyumsuzsa <c>true</c>.</returns>
    public static bool IsMismatch(int totalSeconds, int targetSeconds, decimal tolerance) =>
        totalSeconds < targetSeconds * (1 - tolerance) || totalSeconds > targetSeconds * (1 + tolerance);
}
