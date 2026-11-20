using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Administration.Branches;

/// <summary>Şube formu kuralları (ekleme ve düzenleme ortak): ad zorunlu ve en fazla 100, adres en fazla 300, telefon geçerli.</summary>
public static class BranchFormRules
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> name, Func<T, string?> address, Func<T, string?> phone, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(validator);
        validator.RuleFor(c => name(c)).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.Get(AdministrationMessages.BranchNameRequired))
            .Must(n => n.Trim().Length <= 100).WithMessage(_ => localization.Get(AdministrationMessages.NameTooLong))
            .OverridePropertyName("Name");
        validator.RuleFor(c => address(c)).Must(a => a is null || a.Trim().Length <= 300)
            .WithMessage(_ => localization.Get(AdministrationMessages.AddressTooLong)).OverridePropertyName("Address");
        validator.RuleFor(c => phone(c)).Must(p => PhoneNumber.TryNormalize(p, out _))
            .WithMessage(_ => localization.Get(AdministrationMessages.PhoneInvalid)).OverridePropertyName("Phone");
    }
}
