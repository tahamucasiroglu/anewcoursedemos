using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing.Plans;

/// <summary>
/// Plan formunun alan kuralları (spec 0015 AC-4; önizleme ve açma aynı): öğrenci ve tarife zorunlu, taksit sayısı 1–12, ilk vade zorunlu
/// ve bugün (Türkiye günü, <see cref="SchoolCalendar"/>) ile 365 gün sonrası arasında.
/// </summary>
public static class PlanFormRules
{
    public static void Apply<T>(AbstractValidator<T> validator, ILocalizationService localization, SchoolCalendar calendar)
        where T : IInstallmentPlanForm
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(calendar);
        validator.RuleFor(c => c.StudentId).NotEqual(Guid.Empty)
            .WithMessage(_ => localization.BillingText(BillingMessages.StudentRequired));
        validator.RuleFor(c => c.FeeScheduleId).NotEqual(Guid.Empty)
            .WithMessage(_ => localization.BillingText(BillingMessages.FeeScheduleRequired));
        validator.RuleFor(c => c.InstallmentCount).Must(n => n is { } value && InstallmentSplitter.IsValidCount(value))
            .WithMessage(_ => localization.BillingText(BillingMessages.InstallmentCountInvalid));
        validator.RuleFor(c => c.FirstDueDate).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(_ => localization.BillingText(BillingMessages.FirstDueRequired))
            .Must(d => InstallmentPlan.IsFirstDueAllowed(d!.Value, calendar.Today())).WithMessage(_ => localization.BillingText(BillingMessages.FirstDueOutOfRange));
    }
}
