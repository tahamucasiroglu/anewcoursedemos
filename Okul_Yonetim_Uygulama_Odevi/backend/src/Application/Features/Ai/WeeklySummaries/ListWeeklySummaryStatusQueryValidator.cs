using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>Sınıf listesi (spec 0014): hafta boş (bu hafta), "current" ya da "previous" olmalı.</summary>
public sealed class ListWeeklySummaryStatusQueryValidator : AbstractValidator<ListWeeklySummaryStatusQuery>
{
    public ListWeeklySummaryStatusQueryValidator(ILocalizationService localization) =>
        this.RuleFor(q => q.Week).Must(w => w is null || SchoolWeek.IsValidChoice(w))
            .WithMessage(_ => localization.AiText(AiMessages.SummaryWeekInvalid));
}
