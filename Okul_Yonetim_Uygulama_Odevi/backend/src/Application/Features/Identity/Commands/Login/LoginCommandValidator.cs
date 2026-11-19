using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Identity.Commands.Login;

/// <summary>Giriş alanları: e-posta zorunlu ve geçerli biçimde, şifre zorunlu (uzunluk kuralı girişte uygulanmaz).</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Email).NotEmpty().WithMessage(_ => localization.Get(IdentityMessages.EmailRequired))
            .EmailAddress().WithMessage(_ => localization.Get(IdentityMessages.EmailInvalid));
        this.RuleFor(c => c.Password).NotEmpty().WithMessage(_ => localization.Get(IdentityMessages.PasswordRequired));
    }
}
