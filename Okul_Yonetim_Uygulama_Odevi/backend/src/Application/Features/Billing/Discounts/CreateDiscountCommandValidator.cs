using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Billing.Discounts;

/// <summary>İndirim ekleme (spec 0015 AC-3): ad zorunlu, en fazla 100; yüzde 1–100 tam sayı (0, 101 ve ondalık alan hatası).</summary>
public sealed class CreateDiscountCommandValidator : AbstractValidator<CreateDiscountCommand>
{
    public CreateDiscountCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Name).BillingName(localization);
        this.RuleFor(c => c.Percent).BillingPercent(localization);
    }
}
