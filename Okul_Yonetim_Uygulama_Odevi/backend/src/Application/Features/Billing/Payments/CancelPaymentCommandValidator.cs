using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>Tahsilat iptali (spec 0015 AC-7): gerekçe zorunlu, en fazla 200 karakter (kırpılmış).</summary>
public sealed class CancelPaymentCommandValidator : AbstractValidator<CancelPaymentCommand>
{
    public CancelPaymentCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Reason).Cascade(CascadeMode.Stop)
            .Must(r => !string.IsNullOrWhiteSpace(r)).WithMessage(_ => localization.BillingText(BillingMessages.ReasonRequired))
            .Must(r => r!.Trim().Length <= Payment.ReasonMax).WithMessage(_ => localization.BillingText(BillingMessages.ReasonTooLong));
    }
}
