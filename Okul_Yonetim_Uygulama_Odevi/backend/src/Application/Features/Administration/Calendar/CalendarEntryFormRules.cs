using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.Calendar;

/// <summary>
/// Takvim kaydı formu kuralları (ekleme ve düzenleme ortak): başlık zorunlu ve en fazla 100; tür adıyla
/// (<c>Holiday</c>, <c>Exam</c>, <c>Event</c>); iki gün zorunlu; bitiş başlangıçtan önce olamaz.
/// </summary>
public static class CalendarEntryFormRules
{
    public const int TitleMaxLength = 100;

    public static bool TryParseKind(string? value, out CalendarEntryKind kind)
    {
        kind = default;
        return value is not null
            && Enum.GetNames<CalendarEntryKind>().Contains(value, StringComparer.Ordinal)
            && Enum.TryParse(value, ignoreCase: false, out kind);
    }

    /// <summary>Doğrulamadan geçmiş tür adını çevirir (geçersizse programlama hatası).</summary>
    public static CalendarEntryKind ParseKind(string? value) =>
        TryParseKind(value, out CalendarEntryKind kind) ? kind : throw new ArgumentException("Geçersiz takvim türü.", nameof(value));

    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> title, Func<T, string?> kind, Func<T, DateOnly?> start, Func<T, DateOnly?> end, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(validator);
        validator.RuleFor(c => title(c)).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.Get(AdministrationMessages.CalendarTitleRequired))
            .Must(t => t.Trim().Length <= TitleMaxLength).WithMessage(_ => localization.Get(AdministrationMessages.NameTooLong))
            .OverridePropertyName("Title");
        validator.RuleFor(c => kind(c)).Must(k => TryParseKind(k, out _))
            .WithMessage(_ => localization.Get(AdministrationMessages.CalendarKindInvalid)).OverridePropertyName("Kind");
        validator.RuleFor(c => start(c)).NotNull().WithMessage(_ => localization.Get(AdministrationMessages.DatesRequired)).OverridePropertyName("StartDate");
        validator.RuleFor(c => end(c)).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(_ => localization.Get(AdministrationMessages.DatesRequired))
            .Must((c, e) => start(c) is not { } s || e!.Value >= s).WithMessage(_ => localization.Get(AdministrationMessages.CalendarEndBeforeStart))
            .OverridePropertyName("EndDate");
    }
}
