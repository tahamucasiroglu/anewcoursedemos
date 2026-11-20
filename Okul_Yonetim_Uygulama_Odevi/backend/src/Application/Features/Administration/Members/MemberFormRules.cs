using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>Kişi formu kuralları: ad-soyad zorunlu ve en fazla 100, telefon isteğe bağlı ama geçerli (Ö-3).</summary>
public static class MemberFormRules
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> fullName, Func<T, string?> phone, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(validator);
        validator.RuleFor(c => fullName(c)).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.Get(AdministrationMessages.FullNameRequired))
            .Must(n => n.Trim().Length <= 100).WithMessage(_ => localization.Get(AdministrationMessages.NameTooLong))
            .OverridePropertyName("FullName");
        validator.RuleFor(c => phone(c)).Must(p => PhoneNumber.TryNormalize(p, out _))
            .WithMessage(_ => localization.Get(AdministrationMessages.PhoneInvalid)).OverridePropertyName("Phone");
    }
}
