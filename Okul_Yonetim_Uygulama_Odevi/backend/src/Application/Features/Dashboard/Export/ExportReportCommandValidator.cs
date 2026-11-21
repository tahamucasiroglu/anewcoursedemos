using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Dashboard.Reports;

namespace OkulYonetim.Application.Features.Dashboard.Export;

/// <summary>Dışa aktarma süzgeci (spec 0017 AC-6): rapor türü tanımlı; devamsızlık ve tahsilatta tarih aralığı kuralları (<see cref="ReportFilterRules"/>).</summary>
public sealed class ExportReportCommandValidator : AbstractValidator<ExportReportCommand>
{
    public ExportReportCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Kind).IsInEnum().WithMessage(_ => localization.DashboardText(DashboardMessages.KindInvalid));
        ReportFilterRules.Apply(this, localization, c => c.Kind != ReportKind.Balances);
    }
}
