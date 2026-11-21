using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Rapor süzgeci doğrulaması (spec 0017 AC-6; <see cref="ReportFilterRules"/>): tarih aralığı zorunlu ve en fazla 366 gün; sayfa 1 ya da büyük.</summary>
public sealed class GetAttendanceReportQueryValidator : AbstractValidator<GetAttendanceReportQuery>
{
    public GetAttendanceReportQueryValidator(ILocalizationService localization)
    {
        ReportFilterRules.Apply(this, localization, _ => true);
        this.RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage(_ => localization.DashboardText(DashboardMessages.PageInvalid));
    }
}
