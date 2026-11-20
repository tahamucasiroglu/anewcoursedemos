using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.GradeLevels;

/// <summary>Seviye formu kuralları (ekleme ve düzenleme ortak): ad zorunlu ve en fazla 50, sıra zorunlu ve 1–99.</summary>
public static class GradeLevelFormRules
{
    public const int NameMaxLength = 50;

    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> name, Func<T, int?> sortOrder, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(validator);
        validator.RuleFor(c => name(c)).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.Get(AdministrationMessages.GradeLevelNameRequired))
            .Must(n => n.Trim().Length <= NameMaxLength).WithMessage(_ => localization.Get(AdministrationMessages.GradeLevelNameTooLong))
            .OverridePropertyName("Name");
        validator.RuleFor(c => sortOrder(c)).Must(o => o is >= 1 and <= 99)
            .WithMessage(_ => localization.Get(AdministrationMessages.SortOrderRange)).OverridePropertyName("SortOrder");
    }
}
