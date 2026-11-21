using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Velinin bir çocuğunun ödeme ayrıntısı (spec 0015 gereksinim 10): yalnızca bağlı ve ayrılmamış çocuk; değilse 404 (başka velinin çocuğu dahil).</summary>
public sealed record GetMyChildAccountQuery(Guid StudentId) : IRequest<ChildAccount>;
