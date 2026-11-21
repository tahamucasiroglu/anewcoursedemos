namespace OkulYonetim.Application.Features.Billing;

/// <summary>Tahsilat yazma işleminin sonucu (spec 0015): işlem gören kaydın kimliği (plan açma/iptal, tahsilat iptali).</summary>
public sealed record BillingSaved(Guid Id);
