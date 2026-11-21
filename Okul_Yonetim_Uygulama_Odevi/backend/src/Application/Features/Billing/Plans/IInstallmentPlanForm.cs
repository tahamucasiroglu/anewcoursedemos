namespace OkulYonetim.Application.Features.Billing.Plans;

/// <summary>Plan formunun alanları (spec 0015 gereksinim 3): önizleme ve açma aynı alanları ve aynı doğrulamayı (<see cref="PlanFormRules"/>) paylaşır.</summary>
public interface IInstallmentPlanForm
{
    Guid StudentId { get; }

    Guid FeeScheduleId { get; }

    Guid? DiscountId { get; }

    int? InstallmentCount { get; }

    DateOnly? FirstDueDate { get; }
}
