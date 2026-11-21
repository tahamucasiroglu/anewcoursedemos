using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Communication.WeeklySummaries;

/// <summary>Özet gönderimi (spec 0014 AC-8): hafta boş, "current" ya da "previous"; metin boş olamaz ve kırpılmış hâliyle en fazla 1500 karakter (alan hatası <c>Text</c>).</summary>
public sealed class SendWeeklySummaryCommandValidator : AbstractValidator<SendWeeklySummaryCommand>
{
    public SendWeeklySummaryCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Week).Must(w => w is null || SchoolWeek.IsValidChoice(w))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.WeeklySummaryWeekInvalid));
        this.RuleFor(c => c.Text).Must(t => !string.IsNullOrWhiteSpace(t))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.WeeklySummaryTextRequired));
        this.RuleFor(c => c.Text).Must(t => t!.Trim().Length <= SendWeeklySummaryCommand.TextMax)
            .When(c => !string.IsNullOrWhiteSpace(c.Text))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.WeeklySummaryTextTooLong));
    }
}
