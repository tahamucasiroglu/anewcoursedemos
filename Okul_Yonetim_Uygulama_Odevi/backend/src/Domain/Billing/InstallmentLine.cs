namespace OkulYonetim.Domain.Billing;

/// <summary>Taksit bölmesinin bir satırı (spec 0015 gereksinim 3): sıra (1'den), vade günü ve tutar.</summary>
public sealed record InstallmentLine(int Sequence, DateOnly DueDate, decimal Amount);
