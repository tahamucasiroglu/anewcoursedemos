using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Platform;

/// <summary>Okul ekle formu: dört alan zorunlu, adlar en fazla 100, e-posta biçimi.</summary>
public sealed class CreateSchoolFromPanelCommandValidator : AbstractValidator<CreateSchoolFromPanelCommand>
{
    public CreateSchoolFromPanelCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.SchoolName).Cascade(CascadeMode.Stop).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage(_ => localization.Get(PlatformMessages.SchoolNameRequired))
            .Must(v => v.Trim().Length <= 100).WithMessage(_ => localization.Get(PlatformMessages.NameTooLong));
        this.RuleFor(c => c.BranchName).Cascade(CascadeMode.Stop).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage(_ => localization.Get(PlatformMessages.BranchNameRequired))
            .Must(v => v.Trim().Length <= 100).WithMessage(_ => localization.Get(PlatformMessages.NameTooLong));
        this.RuleFor(c => c.FounderName).Cascade(CascadeMode.Stop).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage(_ => localization.Get(PlatformMessages.FullNameRequired))
            .Must(v => v.Trim().Length <= 100).WithMessage(_ => localization.Get(PlatformMessages.NameTooLong));
        this.RuleFor(c => c.FounderEmail).Cascade(CascadeMode.Stop).NotEmpty().WithMessage(_ => localization.Get(PlatformMessages.EmailRequired))
            .EmailAddress().WithMessage(_ => localization.Get(PlatformMessages.EmailInvalid));
    }
}
