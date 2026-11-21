using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Velinin "Ödemeler" görünümü (spec 0015 gereksinim 10): seçili okuldaki çocuklarının geçerli planları; salt okunur.</summary>
public sealed record ListMyChildrenAccountsQuery : IRequest<MyPayments>;
