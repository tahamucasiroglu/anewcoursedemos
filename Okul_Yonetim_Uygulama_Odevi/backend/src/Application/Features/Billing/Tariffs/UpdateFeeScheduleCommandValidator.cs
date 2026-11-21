using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Billing.Tariffs;

/// <summary>Tarife düzenleme (spec 0015 AC-3): eklemeyle aynı kurallar.</summary>
public sealed class UpdateFeeScheduleCommandValidator : AbstractValidator<UpdateFeeScheduleCommand>
{
    public UpdateFeeScheduleCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Name).BillingName(localization);
        this.RuleFor(c => c.Amount).BillingAmount(localization, feeLimit: true);
    }
}
