using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Plans;

/// <summary>Plan formunun önizlemesi (spec 0015 gereksinim 3): açma ile aynı denetimler, hiçbir şey kaydetmez. Para taşıdığı için loglanmaz (T9).</summary>
public sealed record PreviewInstallmentPlanQuery(Guid StudentId, Guid FeeScheduleId, Guid? DiscountId, int? InstallmentCount, DateOnly? FirstDueDate) : IRequest<PlanPreview>, IInstallmentPlanForm;
