using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;

namespace OkulYonetim.Application.Features.Billing.Plans;

/// <summary>Plan önizlemesi (spec 0015 AC-4): <see cref="PlanFormRules"/>.</summary>
public sealed class PreviewInstallmentPlanQueryValidator : AbstractValidator<PreviewInstallmentPlanQuery>
{
    public PreviewInstallmentPlanQueryValidator(ILocalizationService localization, SchoolCalendar calendar) =>
        PlanFormRules.Apply(this, localization, calendar);
}
