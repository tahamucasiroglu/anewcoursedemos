using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Öğrenci ve veli kayıtları (spec 0009) metinlerinin anahtarları ve okuyucusu (YAML: <c>Features/Students/Resources/Locales/
/// students.tr.yaml</c>). Metin koda gömülmez. Okuyucu adları (<c>StudentTextAsync</c>) yönetim metinlerinin okuyucusuyla
/// çakışmasın diye ayrıdır; doğrulayıcılar eşzamanlı okuyucuyu kullanır.
/// </summary>
public static class StudentsMessages
{
    public const string Section = "Students";
    public const string StudentNotFound = "StudentNotFound";
    public const string ChildNotFound = "ChildNotFound";
    public const string GuardianOnly = "GuardianOnly";
    public const string FullNameRequired = "FullNameRequired";
    public const string FullNameTooLong = "FullNameTooLong";
    public const string BirthDateRequired = "BirthDateRequired";
    public const string BirthDateInFuture = "BirthDateInFuture";
    public const string BirthDateTooOld = "BirthDateTooOld";
    public const string SchoolNumberTooLong = "SchoolNumberTooLong";
    public const string SchoolNumberInvalid = "SchoolNumberInvalid";
    public const string SchoolNumberExists = "SchoolNumberExists";
    public const string BranchRequired = "BranchRequired";
    public const string BranchUnavailable = "BranchUnavailable";
    public const string ClassroomUnavailable = "ClassroomUnavailable";
    public const string BranchCannotChange = "BranchCannotChange";
    public const string HealthNoteTooLong = "HealthNoteTooLong";
    public const string AllergyTooLong = "AllergyTooLong";
    public const string HealthUnavailable = "HealthUnavailable";
    public const string EmailRequired = "EmailRequired";
    public const string EmailInvalid = "EmailInvalid";
    public const string PhoneInvalid = "PhoneInvalid";
    public const string PhoneRequired = "PhoneRequired";
    public const string RelationshipRequired = "RelationshipRequired";
    public const string GuardianAlreadyLinked = "GuardianAlreadyLinked";
    public const string GuardianLimit = "GuardianLimit";
    public const string GuardianInactive = "GuardianInactive";
    public const string GuardianNotLinked = "GuardianNotLinked";
    public const string PickupRelationshipRequired = "PickupRelationshipRequired";
    public const string PickupRelationshipTooLong = "PickupRelationshipTooLong";
    public const string PickupLimit = "PickupLimit";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> StudentTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string StudentText(this ILocalizationService localization, string key) =>
        localization.StudentTextAsync(key).GetAwaiter().GetResult();
}
