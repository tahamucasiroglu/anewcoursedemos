namespace OkulYonetim.Application.Features.Dashboard.Summary;

/// <summary>Geciken ödemeler kartı (spec 0017 gereksinim 4): bugün, gecikmesi olan öğrenci sayısı, toplam geciken tutar ve en eski geciken vadeden başlayarak ilk 10 öğrenci.</summary>
public sealed record OverdueCard(DateOnly Today, int StudentCount, decimal OverdueAmount, IReadOnlyList<OverdueStudent> Top);
