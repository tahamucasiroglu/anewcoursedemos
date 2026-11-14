using FluentValidation;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Application.Resources;

namespace Konsept.Modules.Identity.Application.Validators;

/// <summary>
/// Giriş formu kuralı (spec 0002 Req 10): boş alan alan hatası verir; istek servise ulaşmadığı için deneme sayılmaz.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Initializes a new instance of the <see cref="LoginRequestValidator"/> class.</summary>
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email)
            .Must(e => !string.IsNullOrWhiteSpace(e)).WithErrorCode("identity.email_required").WithMessage(IdentityText.EmailRequired);
        RuleFor(r => r.Password)
            .Must(p => !string.IsNullOrEmpty(p)).WithErrorCode("identity.password_required").WithMessage(IdentityText.PasswordRequired);
    }
}
