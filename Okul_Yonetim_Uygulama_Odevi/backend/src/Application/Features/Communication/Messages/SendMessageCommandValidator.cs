using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Mesaj metni (spec 0011 gereksinim 5, AC-8): boş olamaz; kırpılmış en fazla 2000 karakter (2000 kabul).</summary>
public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Body).Must(b => !string.IsNullOrWhiteSpace(b))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.MessageRequired));
        this.RuleFor(c => c.Body).Must(b => b!.Trim().Length <= ConversationMessage.BodyMaxLength)
            .When(c => !string.IsNullOrWhiteSpace(c.Body))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.MessageTooLong));
    }
}
