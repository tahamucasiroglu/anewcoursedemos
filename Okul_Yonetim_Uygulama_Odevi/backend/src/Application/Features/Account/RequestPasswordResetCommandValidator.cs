using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Administration.Members;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Şifremi unuttum: e-posta zorunlu ve biçimi geçerli.</summary>
public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator(ILocalizationService localization) =>
        this.RuleFor(c => c.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.Get(AccountMessages.EmailRequired))
            .EmailAddress().WithMessage(_ => localization.Get(AccountMessages.EmailInvalid));
}
