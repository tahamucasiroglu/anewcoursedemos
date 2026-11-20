using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Ai.VoiceMessages;

/// <summary>Sesli not alanları (spec 0012 gereksinim 4, AC-5): konuşma zorunlu; ses boş olamaz, en fazla 2 MB (tam 2 MB kabul), türü telefon kaydı biçimlerinden biri.</summary>
public sealed class CreateVoiceMessageDraftCommandValidator : AbstractValidator<CreateVoiceMessageDraftCommand>
{
    public CreateVoiceMessageDraftCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.ConversationId).NotNull()
            .WithMessage(_ => localization.AiText(AiMessages.ConversationRequired));
        this.RuleFor(c => c.Audio).Must(a => a.Length > 0)
            .WithMessage(_ => localization.AiText(AiMessages.AudioRequired));
        this.RuleFor(c => c.Audio).Must(a => a.Length <= CreateVoiceMessageDraftCommand.MaxAudioBytes)
            .WithMessage(_ => localization.AiText(AiMessages.AudioTooLarge));
        this.RuleFor(c => c.ContentType).Must(CreateVoiceMessageDraftCommand.IsSupported)
            .When(c => c.Audio.Length > 0)
            .WithMessage(_ => localization.AiText(AiMessages.AudioTypeInvalid));
    }
}
