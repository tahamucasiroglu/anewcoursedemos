namespace OkulYonetim.Domain.Billing;

/// <summary>Dağıtım hesabının bir satırı (spec 0015 gereksinim 5): hangi taksite ne kadar.</summary>
public sealed record AllocationLine(Guid InstallmentId, decimal Amount);
