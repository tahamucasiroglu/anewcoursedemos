using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>
/// Rapor süzgecinin doğrulama kuralları (spec 0017 gereksinim 6, AC-6): başlangıç ve bitiş zorunlu, başlangıç ≤ bitiş, aralık uçlar dahil
/// en fazla <see cref="MaxRangeDays"/> gün (367 reddedilir, 366 kabul); eşik 0–366. Kural yalnızca tarih isteyen raporlarda tarihleri
/// ister (<c>requiresRange</c>); borç raporu tarihsizdir. Alan adları istekteki adlardır (<c>StartDate</c>, <c>EndDate</c>, <c>MinAbsent</c>).
/// </summary>
public static class ReportFilterRules
{
    public const int MaxRangeDays = 366;
    public const int MaxMinAbsent = 366;

    public static void Apply<T>(AbstractValidator<T> validator, ILocalizationService localization, Func<T, bool> requiresRange)
        where T : IReportFilter
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(requiresRange);
        validator.RuleFor(f => f.StartDate).NotNull().When(requiresRange)
            .WithMessage(_ => localization.DashboardText(DashboardMessages.FromRequired));
        validator.RuleFor(f => f.EndDate).NotNull().When(requiresRange)
            .WithMessage(_ => localization.DashboardText(DashboardMessages.ToRequired));
        validator.RuleFor(f => f.EndDate).Must((f, to) => f.StartDate!.Value <= to!.Value)
            .When(f => requiresRange(f) && f.StartDate is not null && f.EndDate is not null)
            .WithMessage(_ => localization.DashboardText(DashboardMessages.RangeInverted));
        validator.RuleFor(f => f.EndDate).Must((f, to) => RangeDays(f.StartDate!.Value, to!.Value) <= MaxRangeDays)
            .When(f => requiresRange(f) && f.StartDate is not null && f.EndDate is not null && f.StartDate.Value <= f.EndDate.Value)
            .WithMessage(_ => localization.DashboardText(DashboardMessages.RangeTooLong));
        validator.RuleFor(f => f.MinAbsent).InclusiveBetween(0, MaxMinAbsent).When(f => f.MinAbsent is not null)
            .WithMessage(_ => localization.DashboardText(DashboardMessages.MinAbsentInvalid));
    }

    /// <summary>Uçlar dahil gün sayısı (1 Kasım – 1 Kasım = 1).</summary>
    public static int RangeDays(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber + 1;
}
