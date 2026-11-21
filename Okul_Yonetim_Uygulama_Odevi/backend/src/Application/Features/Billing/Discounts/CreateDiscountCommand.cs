using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Discounts;

/// <summary>Kurucunun yeni indirim/burs tanımı (spec 0015 gereksinim 2): ad ve yüzde (ham metin — "15"). Loglanmaz (T9).</summary>
public sealed record CreateDiscountCommand(string? Name, string? Percent) : IRequest<DiscountRow>;
