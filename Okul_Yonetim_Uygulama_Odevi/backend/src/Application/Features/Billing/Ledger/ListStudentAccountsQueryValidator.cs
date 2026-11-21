using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Cari listesi süzgeci (spec 0015): sayfa 1 ya da büyük, arama en fazla 100 karakter.</summary>
public sealed class ListStudentAccountsQueryValidator : AbstractValidator<ListStudentAccountsQuery>
{
    public ListStudentAccountsQueryValidator(ILocalizationService localization)
    {
        this.RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage(_ => localization.BillingText(BillingMessages.PageInvalid));
        this.RuleFor(q => q.Search).Must(s => s!.Trim().Length <= 100).When(q => !string.IsNullOrWhiteSpace(q.Search))
            .WithMessage(_ => localization.BillingText(BillingMessages.SearchTooLong));
    }
}
