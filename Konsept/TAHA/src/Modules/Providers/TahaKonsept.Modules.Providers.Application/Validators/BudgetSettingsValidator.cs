using FluentValidation;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Validators;

/// <summary>
/// <see cref="BudgetSettings"/> doğrulaması: limit 0-100000 $, uyarı eşiği 1-100.
/// </summary>
public sealed class BudgetSettingsValidator : AbstractValidator<BudgetSettings>
{
    /// <summary>Kuralları tanımlar.</summary>
    public BudgetSettingsValidator()
    {
        RuleFor(x => x.MonthlyLimitUsd).InclusiveBetween(0m, 100000m).WithMessage("Aylık limit 0 ile 100000 $ arasında olmalı.");
        RuleFor(x => x.WarnAtPercent).InclusiveBetween(1, 100).WithMessage("Uyarı eşiği 1 ile 100 arasında olmalı.");
    }
}
