using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Onay cevabı: öğrenci zorunlu; cevap yalnızca <c>Approved</c> ya da <c>Declined</c>.</summary>
public sealed class AnswerConsentCommandValidator : AbstractValidator<AnswerConsentCommand>
{
    public AnswerConsentCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.StudentId).NotNull()
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.StudentRequired));
        this.RuleFor(c => c.Answer).Must(a => a is nameof(ConsentAnswer.Approved) or nameof(ConsentAnswer.Declined))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.AnswerInvalid));
    }
}
