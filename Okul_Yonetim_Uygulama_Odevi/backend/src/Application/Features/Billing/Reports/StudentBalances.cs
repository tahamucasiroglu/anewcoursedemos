namespace OkulYonetim.Application.Features.Billing.Reports;

/// <summary>Öğrenci carileri (spec 0017): hesabın yapıldığı gün (Türkiye) ve Türkçe ad sıralı satırlar.</summary>
public sealed record StudentBalances(DateOnly Today, IReadOnlyList<StudentBalanceRow> Rows);
