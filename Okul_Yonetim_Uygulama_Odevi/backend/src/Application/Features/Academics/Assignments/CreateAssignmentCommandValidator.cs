using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Academics;

namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>
/// Ödev verme (spec 0013 C-1): ders adı ve başlık zorunlu (en fazla 60 / 150), açıklama isteğe bağlı (en fazla 2000), teslim
/// günü zorunlu ve bugün (Türkiye günü, <see cref="SchoolCalendar"/>) ile 90 gün sonrası arasında. Boşluklar kırpılarak sayılır.
/// </summary>
public sealed class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentCommandValidator(ILocalizationService localization, SchoolCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        this.RuleFor(c => c.Subject).Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.SubjectRequired));
        this.RuleFor(c => c.Subject).Must(s => s!.Trim().Length <= Assignment.SubjectMax)
            .When(c => !string.IsNullOrWhiteSpace(c.Subject))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.SubjectTooLong));
        this.RuleFor(c => c.Title).Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.AssignmentTitleRequired));
        this.RuleFor(c => c.Title).Must(s => s!.Trim().Length <= Assignment.TitleMax)
            .When(c => !string.IsNullOrWhiteSpace(c.Title))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.AssignmentTitleTooLong));
        this.RuleFor(c => c.Description).Must(s => s!.Trim().Length <= Assignment.DescriptionMax)
            .When(c => !string.IsNullOrWhiteSpace(c.Description))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.DescriptionTooLong));
        this.RuleFor(c => c.DueDate).NotNull()
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.DueDateRequired));
        this.RuleFor(c => c.DueDate).Must(d => Assignment.IsDueDateAllowed(d!.Value, calendar.Today()))
            .When(c => c.DueDate is not null)
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.DueDateOutOfRange));
    }
}
