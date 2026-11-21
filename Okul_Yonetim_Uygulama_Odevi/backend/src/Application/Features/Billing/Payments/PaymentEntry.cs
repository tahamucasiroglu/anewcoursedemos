using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>Tahsilat ve kaydeden kişinin adı (spec 0015 makbuz ve cari: "kaydeden kişinin adı").</summary>
public sealed record PaymentEntry(Payment Payment, string RecordedByName);
