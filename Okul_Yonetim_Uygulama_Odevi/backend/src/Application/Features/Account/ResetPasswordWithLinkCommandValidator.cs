using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Administration.Members;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Bağlantıyla yenileme: bağlantı anahtarı ve yeni şifre kuralı.</summary>
public sealed class ResetPasswordWithLinkCommandValidator : AbstractValidator<ResetPasswordWithLinkCommand>
{
    public ResetPasswordWithLinkCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Token).NotEmpty().WithMessage(_ => localization.Get(AccountMessages.ResetLinkInvalid));
        PasswordRules.Apply(this, c => c.NewPassword, c => c.NewPasswordRepeat, localization);
    }
}
