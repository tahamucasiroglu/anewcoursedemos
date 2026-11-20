using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;

namespace OkulYonetim.Application.Features.Students.Commands.UpdateStudent;

/// <summary>Öğrenci düzenleme alanları: <see cref="StudentFormRules"/>.</summary>
public sealed class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
{
    public UpdateStudentCommandValidator(ILocalizationService localization, SchoolCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        StudentFormRules.Apply(this, c => c.FullName, c => c.BirthDate, c => c.SchoolNumber, calendar.Today, localization);
    }
}
