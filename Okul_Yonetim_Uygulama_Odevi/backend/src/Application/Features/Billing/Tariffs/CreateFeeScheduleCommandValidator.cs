using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Billing.Tariffs;

/// <summary>Tarife ekleme (spec 0015 AC-3): ad zorunlu, en fazla 100; tutar sıfırdan büyük, en fazla 2 ondalık ve 10.000.000,00.</summary>
public sealed class CreateFeeScheduleCommandValidator : AbstractValidator<CreateFeeScheduleCommand>
{
    public CreateFeeScheduleCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Name).BillingName(localization);
        this.RuleFor(c => c.Amount).BillingAmount(localization, feeLimit: true);
    }
}
