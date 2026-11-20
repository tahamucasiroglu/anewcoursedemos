using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.ClassroomTeachers;

/// <summary>Öğretmen listesi: zorunlu (boş olabilir), en fazla <see cref="MaxTeachers"/> kişi, her kişi bir kez.</summary>
public sealed class SetClassroomTeachersCommandValidator : AbstractValidator<SetClassroomTeachersCommand>
{
    public const int MaxTeachers = 20;

    public SetClassroomTeachersCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.TeacherUserIds).NotNull().WithMessage(_ => localization.Get(AdministrationMessages.TeacherListRequired));
        this.RuleFor(c => c.TeacherUserIds!.Count).LessThanOrEqualTo(MaxTeachers)
            .When(c => c.TeacherUserIds is not null)
            .WithMessage(_ => localization.Get(AdministrationMessages.TeachersTooMany));
        this.RuleFor(c => c.TeacherUserIds)
            .Must(ids => ids!.Distinct().Count() == ids!.Count && ids.All(id => id != Guid.Empty))
            .When(c => c.TeacherUserIds is not null)
            .WithMessage(_ => localization.Get(AdministrationMessages.TeacherRepeated));
    }
}
