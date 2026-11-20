using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>"Okundu": gösterilen bildirim kimlikleri zorunlu (en fazla listedeki kadar, <see cref="CommunicationHandlers.MaxListed"/>) ve listenin en yeni zamanı zorunlu.</summary>
public sealed class MarkMyNoticesReadCommandValidator : AbstractValidator<MarkMyNoticesReadCommand>
{
    public MarkMyNoticesReadCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.NoticeIds).NotNull().WithMessage(_ => localization.CommunicationText(CommunicationMessages.ShownNoticesRequired));
        this.RuleFor(c => c.NoticeIds!.Count).LessThanOrEqualTo(CommunicationHandlers.MaxListed)
            .When(c => c.NoticeIds is not null)
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.ShownNoticesRequired));
        this.RuleFor(c => c.UpTo).NotNull().WithMessage(_ => localization.CommunicationText(CommunicationMessages.ShownNoticesRequired));
    }
}
