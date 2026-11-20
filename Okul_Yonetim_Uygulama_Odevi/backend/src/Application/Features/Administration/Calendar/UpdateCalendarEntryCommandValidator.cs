using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Calendar;

/// <summary>Takvim kaydı düzenleme alanları (<see cref="CalendarEntryFormRules"/>).</summary>
public sealed class UpdateCalendarEntryCommandValidator : AbstractValidator<UpdateCalendarEntryCommand>
{
    public UpdateCalendarEntryCommandValidator(ILocalizationService localization) =>
        CalendarEntryFormRules.Apply(this, c => c.Title, c => c.Kind, c => c.StartDate, c => c.EndDate, localization);
}
