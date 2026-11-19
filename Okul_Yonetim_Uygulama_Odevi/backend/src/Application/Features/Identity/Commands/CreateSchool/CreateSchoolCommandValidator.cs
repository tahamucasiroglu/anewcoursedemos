using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Identity.Commands.CreateSchool;

/// <summary>Okul açma alanları: okul, şube, ad-soyad zorunlu (en fazla 200 karakter); e-posta geçerli; verilen şifre en az 8 karakter.</summary>
public sealed class CreateSchoolCommandValidator : AbstractValidator<CreateSchoolCommand>
{
    private const int MaxNameLength = 200;

    public CreateSchoolCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.SchoolName).NotEmpty().WithMessage(_ => localization.Get(IdentityMessages.SchoolNameRequired))
            .MaximumLength(MaxNameLength).WithMessage(_ => localization.Get(IdentityMessages.NameTooLong));
        this.RuleFor(c => c.BranchName).NotEmpty().WithMessage(_ => localization.Get(IdentityMessages.BranchNameRequired))
            .MaximumLength(MaxNameLength).WithMessage(_ => localization.Get(IdentityMessages.NameTooLong));
        this.RuleFor(c => c.FullName).NotEmpty().WithMessage(_ => localization.Get(IdentityMessages.FullNameRequired))
            .MaximumLength(MaxNameLength).WithMessage(_ => localization.Get(IdentityMessages.NameTooLong));
        this.RuleFor(c => c.Email).NotEmpty().WithMessage(_ => localization.Get(IdentityMessages.EmailRequired))
            .EmailAddress().WithMessage(_ => localization.Get(IdentityMessages.EmailInvalid));
        this.RuleFor(c => c.Password).Must(p => p is null || PasswordPolicy.IsSatisfiedBy(p))
            .WithMessage(_ => localization.Get(IdentityMessages.PasswordTooShort));
    }
}
