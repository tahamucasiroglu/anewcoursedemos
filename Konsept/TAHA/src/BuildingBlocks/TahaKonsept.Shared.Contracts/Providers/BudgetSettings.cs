namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Aylık bütçe ayarı (<c>GET|PUT api/providers/budget</c>). Limit aşılınca uyarı verilir; <see cref="StopOnExceed"/> açıksa
/// yeni üretim başlatılamaz (devam eden iş tamamlanır).
/// </summary>
public sealed record BudgetSettings
{
    /// <summary>Aylık limit ($; 0 = limitsiz).</summary>
    public decimal MonthlyLimitUsd { get; init; }

    /// <summary>Aşınca yeni üretimi durdur.</summary>
    public bool StopOnExceed { get; init; } = true;

    /// <summary>Uyarı eşiği (limitin yüzdesi, 1-100; varsayılan 80).</summary>
    public int WarnAtPercent { get; init; } = 80;
}
