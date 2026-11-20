using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Öğrenci formu kuralları (ekleme ve düzenleme ortak, spec 0009 gereksinim 3): ad-soyad zorunlu ve en fazla 100; doğum
/// tarihi zorunlu, bugünden (Türkiye günü) ileri ve 100 yıldan eski olamaz; okul numarası isteğe bağlı, en fazla 20 ve
/// yalnızca harf (A–Z) ve rakam. Sağlık notu en fazla 1000, alerji en fazla 500.
/// </summary>
public static class StudentFormRules
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> fullName, Func<T, DateOnly?> birthDate, Func<T, string?> schoolNumber, Func<DateOnly> today, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(today);
        validator.RuleFor(c => fullName(c)).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localization.StudentText(StudentsMessages.FullNameRequired))
            .Must(n => n.Trim().Length <= Student.FullNameMaxLength).WithMessage(_ => localization.StudentText(StudentsMessages.FullNameTooLong))
            .OverridePropertyName("FullName");
        validator.RuleFor(c => birthDate(c)).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(_ => localization.StudentText(StudentsMessages.BirthDateRequired))
            .Must(d => d!.Value <= today()).WithMessage(_ => localization.StudentText(StudentsMessages.BirthDateInFuture))
            .Must(d => d!.Value >= today().AddYears(-100)).WithMessage(_ => localization.StudentText(StudentsMessages.BirthDateTooOld))
            .OverridePropertyName("BirthDate");
        validator.RuleFor(c => schoolNumber(c)).Cascade(CascadeMode.Stop)
            .Must(n => n is null || n.Trim().Length <= Student.SchoolNumberMaxLength).WithMessage(_ => localization.StudentText(StudentsMessages.SchoolNumberTooLong))
            .Must(n => n is null || n.Trim().All(char.IsAsciiLetterOrDigit)).WithMessage(_ => localization.StudentText(StudentsMessages.SchoolNumberInvalid))
            .OverridePropertyName("SchoolNumber");
    }

    public static void ApplyHealth<T>(AbstractValidator<T> validator, Func<T, string?> healthNote, Func<T, string?> allergy, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(validator);
        validator.RuleFor(c => healthNote(c)).Must(n => n is null || n.Trim().Length <= Student.HealthNoteMaxLength)
            .WithMessage(_ => localization.StudentText(StudentsMessages.HealthNoteTooLong)).OverridePropertyName("HealthNote");
        validator.RuleFor(c => allergy(c)).Must(n => n is null || n.Trim().Length <= Student.AllergyMaxLength)
            .WithMessage(_ => localization.StudentText(StudentsMessages.AllergyTooLong)).OverridePropertyName("Allergy");
    }
}
