using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Discounts;

/// <summary>İndirimin adını ve yüzdesini değiştirir (spec 0015 gereksinim 2); açılmış planlar değişmez. Loglanmaz (T9).</summary>
public sealed record UpdateDiscountCommand(Guid Id, string? Name, string? Percent) : IRequest<DiscountRow>;
