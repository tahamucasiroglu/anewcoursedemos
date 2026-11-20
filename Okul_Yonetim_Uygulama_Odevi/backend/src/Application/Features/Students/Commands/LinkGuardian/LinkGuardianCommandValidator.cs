using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Application.Features.Students.Commands.LinkGuardian;

/// <summary>Veli alanları: ad-soyad zorunlu ve en fazla 100, e-posta zorunlu ve geçerli, telefon isteğe bağlı ama geçerli, yakınlık seçili.</summary>
public sealed class LinkGuardianCommandValidator : AbstractValidator<LinkGuardianCommand>
{
    public LinkGuardianCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.FullName).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.StudentText(StudentsMessages.FullNameRequired))
            .Must(n => n.Trim().Length <= Student.FullNameMaxLength).WithMessage(_ => localization.StudentText(StudentsMessages.FullNameTooLong));
        this.RuleFor(c => c.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.StudentText(StudentsMessages.EmailRequired))
            .EmailAddress().WithMessage(_ => localization.StudentText(StudentsMessages.EmailInvalid));
        this.RuleFor(c => c.Phone).Must(p => PhoneNumber.TryNormalize(p, out _)).WithMessage(_ => localization.StudentText(StudentsMessages.PhoneInvalid));
        this.RuleFor(c => c.Relationship).Must(r => TryParseRelationship(r, out _)).WithMessage(_ => localization.StudentText(StudentsMessages.RelationshipRequired));
    }

    public static bool TryParseRelationship(string? value, out GuardianRelationship relationship) =>
        Enum.TryParse(value, ignoreCase: false, out relationship) && Enum.IsDefined(relationship) && !int.TryParse(value, out _);
}
