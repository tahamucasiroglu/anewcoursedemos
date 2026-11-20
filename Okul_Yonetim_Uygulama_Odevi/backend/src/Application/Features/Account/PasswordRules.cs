using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Yeni şifre kuralı (spec 0004: en az 8 karakter) ve tekrarıyla eşleşme (spec 0006).</summary>
public static class PasswordRules
{
    public const int MinLength = 8;

    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> password, Func<T, string> repeat, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(validator);
        validator.RuleFor(c => password(c)).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.Get(AccountMessages.PasswordRequired))
            .MinimumLength(MinLength).WithMessage(_ => localization.Get(AccountMessages.PasswordTooShort))
            .OverridePropertyName("NewPassword");
        validator.RuleFor(c => repeat(c)).Equal(c => password(c)).WithMessage(_ => localization.Get(AccountMessages.PasswordMismatch))
            .OverridePropertyName("NewPasswordRepeat");
    }
}
