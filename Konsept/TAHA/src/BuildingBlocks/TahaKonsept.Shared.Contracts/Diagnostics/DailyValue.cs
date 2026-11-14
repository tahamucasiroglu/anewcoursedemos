namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Günlük seri noktası (Türkiye saatine göre gün): <c>{ date: "2026-11-14", value: 3 }</c>. Grafiklerde kullanılır.
/// </summary>
public sealed record DailyValue
{
    /// <summary>Gün.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Değer (adet ya da dolar).</summary>
    public decimal Value { get; init; }
}
