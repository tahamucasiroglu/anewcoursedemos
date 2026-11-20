using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.GradeLevels;

/// <summary>Seviye ekleme alanları (<see cref="GradeLevelFormRules"/>).</summary>
public sealed class CreateGradeLevelCommandValidator : AbstractValidator<CreateGradeLevelCommand>
{
    public CreateGradeLevelCommandValidator(ILocalizationService localization) =>
        GradeLevelFormRules.Apply(this, c => c.Name, c => c.SortOrder, localization);
}
