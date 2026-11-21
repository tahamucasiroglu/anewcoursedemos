using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Discounts;

/// <summary>Okulun indirim/burs tanımları (spec 0015): kurucu yönetir, muhasebe plan formu için okur; önce etkinler, Türkçe ad sırasıyla.</summary>
public sealed record ListDiscountsQuery : IRequest<IReadOnlyList<DiscountRow>>;
