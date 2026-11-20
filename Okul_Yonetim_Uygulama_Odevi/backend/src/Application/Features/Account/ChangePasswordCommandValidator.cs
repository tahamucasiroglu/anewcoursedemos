using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Administration.Members;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Şifre değiştirme alanları: mevcut şifre zorunlu, yeni şifre kuralı, tekrarı eşleşir, mevcuttan farklı.</summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.CurrentPassword).NotEmpty().WithMessage(_ => localization.Get(AccountMessages.CurrentPasswordRequired));
        PasswordRules.Apply(this, c => c.NewPassword, c => c.NewPasswordRepeat, localization);
        this.RuleFor(c => c.NewPassword).NotEqual(c => c.CurrentPassword, StringComparer.Ordinal)
            .When(c => !string.IsNullOrEmpty(c.NewPassword))
            .WithMessage(_ => localization.Get(AccountMessages.PasswordSameAsCurrent));
    }
}
