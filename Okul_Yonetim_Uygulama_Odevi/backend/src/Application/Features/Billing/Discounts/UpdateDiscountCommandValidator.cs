using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Billing.Discounts;

/// <summary>İndirim düzenleme (spec 0015 AC-3): eklemeyle aynı kurallar.</summary>
public sealed class UpdateDiscountCommandValidator : AbstractValidator<UpdateDiscountCommand>
{
    public UpdateDiscountCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Name).BillingName(localization);
        this.RuleFor(c => c.Percent).BillingPercent(localization);
    }
}
