using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Calendar;

/// <summary>Takvim kaydı ekleme alanları (<see cref="CalendarEntryFormRules"/>).</summary>
public sealed class CreateCalendarEntryCommandValidator : AbstractValidator<CreateCalendarEntryCommand>
{
    public CreateCalendarEntryCommandValidator(ILocalizationService localization) =>
        CalendarEntryFormRules.Apply(this, c => c.Title, c => c.Kind, c => c.StartDate, c => c.EndDate, localization);
}
