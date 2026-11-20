using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>"Okundu": listenin en yeni zamanı zorunlu.</summary>
public sealed class MarkMyNoticesReadCommandValidator : AbstractValidator<MarkMyNoticesReadCommand>
{
    public MarkMyNoticesReadCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.UpTo).NotNull().WithMessage(_ => localization.CommunicationText(CommunicationMessages.ShownNoticesRequired));
    }
}
