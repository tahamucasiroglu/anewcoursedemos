using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;

namespace OkulYonetim.Application.Features.Students.Commands.CreateStudent;

/// <summary>Öğrenci ekleme alanları: <see cref="StudentFormRules"/>, şube seçimi zorunlu, sağlık alanlarının uzunluğu.</summary>
public sealed class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator(ILocalizationService localization, SchoolCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        StudentFormRules.Apply(this, c => c.FullName, c => c.BirthDate, c => c.SchoolNumber, calendar.Today, localization);
        StudentFormRules.ApplyHealth(this, c => c.HealthNote, c => c.Allergy, localization);
        this.RuleFor(c => c.BranchId).Must(id => id is { } b && b != Guid.Empty).WithMessage(_ => localization.StudentText(StudentsMessages.BranchRequired));
    }
}
