using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.Terms;

/// <summary>Dönem formu kuralları (ekleme ve düzenleme ortak): ad zorunlu ve en fazla 100; iki gün zorunlu; bitiş başlangıçtan sonra; en fazla 366 gün.</summary>
public static class TermFormRules
{
    public const int NameMaxLength = 100;

    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> name, Func<T, DateOnly?> start, Func<T, DateOnly?> end, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(validator);
        validator.RuleFor(c => name(c)).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.Get(AdministrationMessages.TermNameRequired))
            .Must(n => n.Trim().Length <= NameMaxLength).WithMessage(_ => localization.Get(AdministrationMessages.NameTooLong))
            .OverridePropertyName("Name");
        validator.RuleFor(c => start(c)).NotNull().WithMessage(_ => localization.Get(AdministrationMessages.DatesRequired)).OverridePropertyName("StartDate");
        validator.RuleFor(c => end(c)).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(_ => localization.Get(AdministrationMessages.DatesRequired))
            .Must((c, e) => start(c) is not { } s || e!.Value > s).WithMessage(_ => localization.Get(AdministrationMessages.TermEndBeforeStart))
            .Must((c, e) => start(c) is not { } s || Term.LengthInDays(s, e!.Value) <= Term.MaxDays).WithMessage(_ => localization.Get(AdministrationMessages.TermTooLong))
            .OverridePropertyName("EndDate");
    }
}
