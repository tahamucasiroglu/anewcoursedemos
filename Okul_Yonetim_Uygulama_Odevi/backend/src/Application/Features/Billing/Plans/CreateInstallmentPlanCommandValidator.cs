using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;

namespace OkulYonetim.Application.Features.Billing.Plans;

/// <summary>Plan açma (spec 0015 AC-4): <see cref="PlanFormRules"/> (taksit sayısı 0/13, dünkü ya da 366 gün sonraki ilk vade alan hatası).</summary>
public sealed class CreateInstallmentPlanCommandValidator : AbstractValidator<CreateInstallmentPlanCommand>
{
    public CreateInstallmentPlanCommandValidator(ILocalizationService localization, SchoolCalendar calendar) =>
        PlanFormRules.Apply(this, localization, calendar);
}
