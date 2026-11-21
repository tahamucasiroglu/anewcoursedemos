using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Reports;

/// <summary>
/// Tahsilat günü aralıkta (uçlar dahil) olan bütün tahsilatlar, iptaller dahil, tarih ve makbuz no sırasıyla (spec 0017 gereksinim 8); şube
/// süzgeci. Uç yoktur ve erişim denetlemez — çağıran denetler.
/// </summary>
public sealed record ListPaymentsInRangeQuery(DateOnly FromDay, DateOnly ToDay, Guid? BranchId) : IRequest<IReadOnlyList<PaymentRangeRow>>;
