using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Application.Features.Students.Commands.SetPickupPersons;

/// <summary>Teslim kişileri: en fazla 10; boş (null) kişi olamaz; her kişide ad-soyad (≤ 100), yakınlık (≤ 50) ve geçerli telefon zorunlu.</summary>
public sealed class SetPickupPersonsCommandValidator : AbstractValidator<SetPickupPersonsCommand>
{
    public SetPickupPersonsCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Persons).Must(p => p is null || p.Count <= PickupPerson.MaxPerStudent)
            .WithMessage(_ => localization.StudentText(StudentsMessages.PickupLimit));
        this.RuleForEach(c => c.Persons).NotNull().WithMessage(_ => localization.StudentText(StudentsMessages.PickupPersonRequired));
        this.RuleForEach(c => c.Persons).ChildRules(person =>
        {
            person.RuleFor(p => p.FullName).Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(_ => localization.StudentText(StudentsMessages.FullNameRequired))
                .Must(n => n.Trim().Length <= PickupPerson.FullNameMaxLength).WithMessage(_ => localization.StudentText(StudentsMessages.FullNameTooLong));
            person.RuleFor(p => p.Relationship).Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(_ => localization.StudentText(StudentsMessages.PickupRelationshipRequired))
                .Must(r => r.Trim().Length <= PickupPerson.RelationshipMaxLength).WithMessage(_ => localization.StudentText(StudentsMessages.PickupRelationshipTooLong));
            person.RuleFor(p => p.Phone).Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(_ => localization.StudentText(StudentsMessages.PhoneRequired))
                .Must(p => PhoneNumber.TryNormalize(p, out _)).WithMessage(_ => localization.StudentText(StudentsMessages.PhoneInvalid));
        });
    }
}
