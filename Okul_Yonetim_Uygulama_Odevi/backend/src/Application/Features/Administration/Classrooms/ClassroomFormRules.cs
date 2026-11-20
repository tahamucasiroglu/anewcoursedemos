using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>Sınıf formu kuralları: ad zorunlu ve en fazla 30; seviye seçimi zorunlu (açarken ayrıca dönem ve şube).</summary>
public static class ClassroomFormRules
{
    public const int NameMaxLength = 30;

    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> name, Func<T, Guid?> gradeLevelId, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(validator);
        validator.RuleFor(c => name(c)).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.Get(AdministrationMessages.ClassroomNameRequired))
            .Must(n => n.Trim().Length <= NameMaxLength).WithMessage(_ => localization.Get(AdministrationMessages.ClassroomNameTooLong))
            .OverridePropertyName("Name");
        validator.RuleFor(c => gradeLevelId(c)).Must(id => id is { } g && g != Guid.Empty)
            .WithMessage(_ => localization.Get(AdministrationMessages.GradeLevelRequired)).OverridePropertyName("GradeLevelId");
    }
}
