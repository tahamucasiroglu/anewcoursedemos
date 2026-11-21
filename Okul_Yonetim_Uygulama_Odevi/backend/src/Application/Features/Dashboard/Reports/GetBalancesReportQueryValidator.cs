using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Rapor süzgeci doğrulaması (spec 0017 AC-6; <see cref="ReportFilterRules"/>): borç raporu tarihsiz; sayfa 1 ya da büyük.</summary>
public sealed class GetBalancesReportQueryValidator : AbstractValidator<GetBalancesReportQuery>
{
    public GetBalancesReportQueryValidator(ILocalizationService localization)
    {
        ReportFilterRules.Apply(this, localization, _ => false);
        this.RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage(_ => localization.DashboardText(DashboardMessages.PageInvalid));
    }
}
