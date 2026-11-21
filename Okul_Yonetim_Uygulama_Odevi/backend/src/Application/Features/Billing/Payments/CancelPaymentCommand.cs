using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>Tahsilatı gerekçeyle iptal eder (spec 0015 gereksinim 6). Gerekçe metni taşıdığı için loglanmaz (T9).</summary>
public sealed record CancelPaymentCommand(Guid PaymentId, string? Reason) : IRequest<BillingSaved>;
