using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Plans;

/// <summary>Geçerli tahsilatı olmayan planı iptal eder (spec 0015 gereksinim 4).</summary>
public sealed record CancelInstallmentPlanCommand(Guid PlanId) : IRequest<BillingSaved>;
