using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>Kişi ekleme alanları: ad-soyad, e-posta (zorunlu, geçerli biçim), telefon, en az bir rol.</summary>
public sealed class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator(ILocalizationService localization)
    {
        MemberFormRules.Apply(this, c => c.FullName, c => c.Phone, localization);
        this.RuleFor(c => c.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.Get(AdministrationMessages.EmailRequired))
            .EmailAddress().WithMessage(_ => localization.Get(AdministrationMessages.EmailInvalid));
        this.RuleFor(c => c.Roles).Must(r => r is { Count: > 0 }).WithMessage(_ => localization.Get(AdministrationMessages.RoleRequired));
    }
}
