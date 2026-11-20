using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>Sınıf düzenleme alanları (<see cref="ClassroomFormRules"/>).</summary>
public sealed class UpdateClassroomCommandValidator : AbstractValidator<UpdateClassroomCommand>
{
    public UpdateClassroomCommandValidator(ILocalizationService localization) =>
        ClassroomFormRules.Apply(this, c => c.Name, c => c.GradeLevelId, localization);
}
