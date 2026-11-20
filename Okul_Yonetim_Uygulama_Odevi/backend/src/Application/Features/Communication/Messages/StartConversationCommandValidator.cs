using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Konuşma başlatma: öğrenci ve karşı taraf zorunlu.</summary>
public sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.StudentId).NotNull()
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.StudentRequired));
        this.RuleFor(c => c.CounterpartUserId).NotNull()
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.CounterpartRequired));
    }
}
