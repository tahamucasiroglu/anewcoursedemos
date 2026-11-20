using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.GradeLevels;

/// <summary>Seviye düzenleme alanları (<see cref="GradeLevelFormRules"/>).</summary>
public sealed class UpdateGradeLevelCommandValidator : AbstractValidator<UpdateGradeLevelCommand>
{
    public UpdateGradeLevelCommandValidator(ILocalizationService localization) =>
        GradeLevelFormRules.Apply(this, c => c.Name, c => c.SortOrder, localization);
}
