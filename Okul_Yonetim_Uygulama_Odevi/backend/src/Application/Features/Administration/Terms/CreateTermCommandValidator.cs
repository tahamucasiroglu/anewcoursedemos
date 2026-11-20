using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Terms;

/// <summary>Dönem ekleme alanları (<see cref="TermFormRules"/>).</summary>
public sealed class CreateTermCommandValidator : AbstractValidator<CreateTermCommand>
{
    public CreateTermCommandValidator(ILocalizationService localization) =>
        TermFormRules.Apply(this, c => c.Name, c => c.StartDate, c => c.EndDate, localization);
}
