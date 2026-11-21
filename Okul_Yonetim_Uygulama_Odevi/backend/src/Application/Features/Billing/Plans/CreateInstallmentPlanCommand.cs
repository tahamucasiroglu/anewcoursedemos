using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Plans;

/// <summary>Öğrenciye taksit planı açar (spec 0015 gereksinim 3): tarife, isteğe bağlı indirim, taksit sayısı (1–12) ve ilk vade. Loglanmaz (T9).</summary>
public sealed record CreateInstallmentPlanCommand(Guid StudentId, Guid FeeScheduleId, Guid? DiscountId, int? InstallmentCount, DateOnly? FirstDueDate) : IRequest<BillingSaved>, IInstallmentPlanForm;
