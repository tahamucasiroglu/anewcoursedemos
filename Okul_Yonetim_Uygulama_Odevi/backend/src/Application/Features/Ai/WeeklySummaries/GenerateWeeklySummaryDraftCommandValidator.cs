using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>"Özet oluştur" (spec 0014 AC-8): hafta boş, "current" ya da "previous"; öğretmen notu isteğe bağlı, kırpılmış hâliyle en fazla 500 karakter (alan hatası <c>TeacherNote</c>).</summary>
public sealed class GenerateWeeklySummaryDraftCommandValidator : AbstractValidator<GenerateWeeklySummaryDraftCommand>
{
    public GenerateWeeklySummaryDraftCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Week).Must(w => w is null || SchoolWeek.IsValidChoice(w))
            .WithMessage(_ => localization.AiText(AiMessages.SummaryWeekInvalid));
        this.RuleFor(c => c.TeacherNote).Must(n => n!.Trim().Length <= GenerateWeeklySummaryDraftCommand.TeacherNoteMax)
            .When(c => c.TeacherNote is not null)
            .WithMessage(_ => localization.AiText(AiMessages.SummaryNoteTooLong));
    }
}
