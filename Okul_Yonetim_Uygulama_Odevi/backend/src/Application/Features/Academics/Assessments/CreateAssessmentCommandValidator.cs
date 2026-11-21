using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Academics;

namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>
/// Değerlendirme açma (spec 0013 C-3): ders adı ve başlık zorunlu (en fazla 60 / 100), tür <c>Written</c> ya da <c>Oral</c>,
/// gün zorunlu ve bugünden (Türkiye günü) sonra olamaz. Günün sınıfın dönemi içinde olması işleyicide denetlenir (dönem
/// veritabanından okunur) ve aynı biçimde alan hatası olarak döner.
/// </summary>
public sealed class CreateAssessmentCommandValidator : AbstractValidator<CreateAssessmentCommand>
{
    public CreateAssessmentCommandValidator(ILocalizationService localization, SchoolCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        this.RuleFor(c => c.Subject).Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.SubjectRequired));
        this.RuleFor(c => c.Subject).Must(s => s!.Trim().Length <= Assessment.SubjectMax)
            .When(c => !string.IsNullOrWhiteSpace(c.Subject))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.SubjectTooLong));
        this.RuleFor(c => c.Title).Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.AssessmentTitleRequired));
        this.RuleFor(c => c.Title).Must(s => s!.Trim().Length <= Assessment.TitleMax)
            .When(c => !string.IsNullOrWhiteSpace(c.Title))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.AssessmentTitleTooLong));
        this.RuleFor(c => c.Kind).Must(k => TryParseKind(k, out _))
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.KindInvalid));
        this.RuleFor(c => c.Date).NotNull()
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.DateRequired));
        this.RuleFor(c => c.Date).Must(d => d!.Value <= calendar.Today())
            .When(c => c.Date is not null)
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.DateInFuture));
    }

    /// <summary>API'deki tür metni (<c>Written</c>, <c>Oral</c>; harf farkı gözetilmez, sayı kabul edilmez) → tür.</summary>
    public static bool TryParseKind(string? text, out AssessmentKind kind)
    {
        kind = default;
        return text is not null
            && !int.TryParse(text, out _)
            && Enum.TryParse(text, ignoreCase: true, out kind)
            && Enum.IsDefined(kind);
    }
}
