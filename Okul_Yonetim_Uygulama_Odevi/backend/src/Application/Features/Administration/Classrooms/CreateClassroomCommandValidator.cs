using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>Sınıf açma alanları: dönem ve şube seçimi zorunlu, ad ve seviye <see cref="ClassroomFormRules"/>.</summary>
public sealed class CreateClassroomCommandValidator : AbstractValidator<CreateClassroomCommand>
{
    public CreateClassroomCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.TermId).Must(id => id is { } t && t != Guid.Empty).WithMessage(_ => localization.Get(AdministrationMessages.TermRequired));
        this.RuleFor(c => c.BranchId).Must(id => id is { } b && b != Guid.Empty).WithMessage(_ => localization.Get(AdministrationMessages.BranchSelectRequired));
        ClassroomFormRules.Apply(this, c => c.Name, c => c.GradeLevelId, localization);
    }
}
