using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Students.Commands.SetStudentHealth;

/// <summary>Sağlık alanlarının uzunluğu: sağlık notu en fazla 1000, alerji en fazla 500.</summary>
public sealed class SetStudentHealthCommandValidator : AbstractValidator<SetStudentHealthCommand>
{
    public SetStudentHealthCommandValidator(ILocalizationService localization) =>
        StudentFormRules.ApplyHealth(this, c => c.HealthNote, c => c.Allergy, localization);
}
