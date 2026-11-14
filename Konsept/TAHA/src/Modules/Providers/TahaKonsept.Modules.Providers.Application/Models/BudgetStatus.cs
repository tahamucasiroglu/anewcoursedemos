namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Bir kullanıcının bu ayki bütçe durumu (<c>IBudgetGuard.GetStatusAsync</c>).
/// </summary>
public sealed record BudgetStatus
{
    /// <summary>Ay (<c>yyyy-MM</c>, Türkiye saati).</summary>
    public string Month { get; init; } = string.Empty;

    /// <summary>Bu ayki harcama ($).</summary>
    public decimal SpentUsd { get; init; }

    /// <summary>Limit ($; 0 = limitsiz).</summary>
    public decimal LimitUsd { get; init; }

    /// <summary>Aşınca yeni üretim durdurulsun mu.</summary>
    public bool StopOnExceed { get; init; }

    /// <summary>Uyarı eşiği (%).</summary>
    public int WarnAtPercent { get; init; }

    /// <summary>Limit aşıldı mı (limit 0 ise hiçbir zaman).</summary>
    public bool Exceeded => LimitUsd > 0 && SpentUsd >= LimitUsd;

    /// <summary>Uyarı eşiğine ulaşıldı mı.</summary>
    public bool Warning => LimitUsd > 0 && SpentUsd >= LimitUsd * WarnAtPercent / 100m;

    /// <summary>Yeni üretim engelleniyor mu.</summary>
    public bool BlocksNewProduction => Exceeded && StopOnExceed;
}
