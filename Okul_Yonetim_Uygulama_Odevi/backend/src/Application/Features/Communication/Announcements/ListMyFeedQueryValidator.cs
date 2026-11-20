using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Veli akışı: sayfa 1 ile <see cref="MaxPage"/> arasında (review L-3 — büyük sayfa numarası taşma ve 500 yerine 400).</summary>
public sealed class ListMyFeedQueryValidator : AbstractValidator<ListMyFeedQuery>
{
    public const int MaxPage = 500;

    public ListMyFeedQueryValidator(ILocalizationService localization)
    {
        this.RuleFor(q => q.Page).InclusiveBetween(1, MaxPage)
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.PageRange));
    }
}
