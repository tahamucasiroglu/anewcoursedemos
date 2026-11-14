namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Bu ayın harcaması (<c>GET api/providers/usage/monthly</c>; üst bardaki harcama rozeti). Ay sınırı Türkiye saatine göredir.
/// </summary>
public sealed record MonthlyUsageResponse
{
    /// <summary>Ay (<c>yyyy-MM</c>).</summary>
    public string Month { get; init; } = string.Empty;

    /// <summary>Bu ayki toplam harcama ($; LLM + ses + görsel).</summary>
    public decimal TotalUsd { get; init; }

    /// <summary>Aylık limit ($; 0 = limitsiz).</summary>
    public decimal LimitUsd { get; init; }

    /// <summary>Limit aşıldı mı.</summary>
    public bool Exceeded { get; init; }

    /// <summary>Uyarı eşiğine ulaşıldı mı (limitin uyarı yüzdesi).</summary>
    public bool Warning { get; init; }

    /// <summary>Aşınca yeni üretim durduruluyor mu.</summary>
    public bool StopOnExceed { get; init; }

    /// <summary>Platform kırılımı (pahalıdan ucuza).</summary>
    public IReadOnlyList<PlatformUsage> ByPlatform { get; init; } = [];
}
