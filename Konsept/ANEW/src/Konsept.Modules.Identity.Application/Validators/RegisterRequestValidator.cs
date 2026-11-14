using FluentValidation;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Application.Resources;
using Konsept.Modules.Identity.Domain.Entities;

namespace Konsept.Modules.Identity.Application.Validators;

/// <summary>
/// Kayıt kuralları (spec 0002 Req 3): ad kırpıldıktan sonra 1–100 karakter; e-posta geçerli biçimde, en fazla 254
/// karakter; şifre 8–128 karakter, en az bir harf ve bir rakam. Her alan kendi Türkçe mesajını verir.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>Şifrenin en az uzunluğu.</summary>
    public const int PasswordMinLength = 8;

    /// <summary>Şifrenin en fazla uzunluğu.</summary>
    public const int PasswordMaxLength = 128;

    /// <summary>Initializes a new instance of the <see cref="RegisterRequestValidator"/> class.</summary>
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithErrorCode("identity.name_required").WithMessage(IdentityText.NameRequired)
            .Must(n => (n ?? string.Empty).Trim().Length <= User.NameMaxLength).WithErrorCode("identity.name_too_long").WithMessage(IdentityText.NameTooLong);
        RuleFor(r => r.Email)
            .Cascade(CascadeMode.Stop)
            .Must(e => !string.IsNullOrWhiteSpace(e)).WithErrorCode("identity.email_required").WithMessage(IdentityText.EmailRequired)
            .Must(e => e!.Trim().Length <= User.EmailMaxLength).WithErrorCode("identity.email_too_long").WithMessage(IdentityText.EmailTooLong)
            .Must(EmailFormat.IsValid).WithErrorCode("identity.email_invalid").WithMessage(IdentityText.EmailInvalid);
        RuleFor(r => r.Password)
            .Cascade(CascadeMode.Stop)
            .Must(p => !string.IsNullOrEmpty(p)).WithErrorCode("identity.password_required").WithMessage(IdentityText.PasswordRequired)
            .Must(p => p!.Length is >= PasswordMinLength and <= PasswordMaxLength).WithErrorCode("identity.password_length").WithMessage(IdentityText.PasswordLength)
            .Must(p => p!.Any(char.IsLetter) && p!.Any(char.IsDigit)).WithErrorCode("identity.password_letter_digit").WithMessage(IdentityText.PasswordLetterAndDigit);
    }
}
