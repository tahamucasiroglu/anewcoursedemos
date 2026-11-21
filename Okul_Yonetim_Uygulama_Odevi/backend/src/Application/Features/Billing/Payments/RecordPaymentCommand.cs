using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>Elle tahsilat kaydı (spec 0015 gereksinim 5): plan, tutar (ham metin), tarih, yöntem (<c>Cash</c>/<c>BankTransfer</c>/<c>CardPos</c>) ve isteğe bağlı not. Para ve not taşıdığı için loglanmaz (T9).</summary>
public sealed record RecordPaymentCommand(Guid PlanId, string? Amount, DateOnly? Date, string? Method, string? Note) : IRequest<PaymentRecorded>;
