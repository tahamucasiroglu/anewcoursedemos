using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>"Vazgeç" (spec 0014): hafta boş (bu hafta), "current" ya da "previous" olmalı.</summary>
public sealed class DiscardWeeklySummaryDraftCommandValidator : AbstractValidator<DiscardWeeklySummaryDraftCommand>
{
    public DiscardWeeklySummaryDraftCommandValidator(ILocalizationService localization) =>
        this.RuleFor(c => c.Week).Must(w => w is null || SchoolWeek.IsValidChoice(w))
            .WithMessage(_ => localization.AiText(AiMessages.SummaryWeekInvalid));
}
