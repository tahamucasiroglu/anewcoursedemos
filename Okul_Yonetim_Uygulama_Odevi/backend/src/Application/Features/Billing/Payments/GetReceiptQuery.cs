using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>Tahsilatın makbuzu (spec 0015 gereksinim 7).</summary>
public sealed record GetReceiptQuery(Guid PaymentId) : IRequest<Receipt>;
