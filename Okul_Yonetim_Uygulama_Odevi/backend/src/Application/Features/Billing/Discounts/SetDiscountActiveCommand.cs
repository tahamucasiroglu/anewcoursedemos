using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Discounts;

/// <summary>İndirimi pasifleştirir ya da yeniden etkinleştirir (spec 0015 gereksinim 2); pasif indirim yeni planda seçilemez.</summary>
public sealed record SetDiscountActiveCommand(Guid Id, bool Active) : IRequest<DiscountRow>;
