using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Terms;

/// <summary>Dönem düzenleme alanları (<see cref="TermFormRules"/>).</summary>
public sealed class UpdateTermCommandValidator : AbstractValidator<UpdateTermCommand>
{
    public UpdateTermCommandValidator(ILocalizationService localization) =>
        TermFormRules.Apply(this, c => c.Name, c => c.StartDate, c => c.EndDate, localization);
}
