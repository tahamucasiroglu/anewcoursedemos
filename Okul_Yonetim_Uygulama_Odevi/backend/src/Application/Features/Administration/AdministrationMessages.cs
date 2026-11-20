using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration;

/// <summary>
/// Yönetim (spec 0005, akademik yapı spec 0008, sınıf öğretmeni spec 0010) metinlerinin anahtarları ve okuyucusu (YAML: <c>Features/Administration/Resources/Locales/
/// administration.tr.yaml</c>). Metin koda gömülmez; doğrulayıcılar eşzamanlı okuyucuyu kullanır.
/// </summary>
public static class AdministrationMessages
{
    public const string Section = "Administration";
    public const string Forbidden = "Forbidden";
    public const string BranchNameRequired = "BranchNameRequired";
    public const string NameTooLong = "NameTooLong";
    public const string AddressTooLong = "AddressTooLong";
    public const string PhoneInvalid = "PhoneInvalid";
    public const string BranchNameExists = "BranchNameExists";
    public const string BranchNotFound = "BranchNotFound";
    public const string LastActiveBranch = "LastActiveBranch";
    public const string FullNameRequired = "FullNameRequired";
    public const string EmailRequired = "EmailRequired";
    public const string EmailInvalid = "EmailInvalid";
    public const string RoleRequired = "RoleRequired";
    public const string RoleInvalid = "RoleInvalid";
    public const string BranchRequiredForRole = "BranchRequiredForRole";
    public const string BranchUnavailable = "BranchUnavailable";
    public const string MemberNotFound = "MemberNotFound";
    public const string MemberAlreadyInTenant = "MemberAlreadyInTenant";
    public const string CannotRemoveOwnFounder = "CannotRemoveOwnFounder";
    public const string CannotDeactivateSelf = "CannotDeactivateSelf";
    public const string LastFounder = "LastFounder";
    public const string ResetBlockedOtherTenant = "ResetBlockedOtherTenant";
    public const string ProfileBlockedOtherTenant = "ProfileBlockedOtherTenant";
    public const string TooManyRequests = "TooManyRequests";
    public const string GradeLevelNameRequired = "GradeLevelNameRequired";
    public const string GradeLevelNameTooLong = "GradeLevelNameTooLong";
    public const string SortOrderRange = "SortOrderRange";
    public const string GradeLevelNameExists = "GradeLevelNameExists";
    public const string GradeLevelNotFound = "GradeLevelNotFound";
    public const string GradeLevelUnavailable = "GradeLevelUnavailable";
    public const string TermNameRequired = "TermNameRequired";
    public const string DatesRequired = "DatesRequired";
    public const string TermEndBeforeStart = "TermEndBeforeStart";
    public const string TermTooLong = "TermTooLong";
    public const string TermNameExists = "TermNameExists";
    public const string TermOverlaps = "TermOverlaps";
    public const string TermLeavesEntriesOutside = "TermLeavesEntriesOutside";
    public const string TermNotFound = "TermNotFound";
    public const string TermRequired = "TermRequired";
    public const string ClassroomNameRequired = "ClassroomNameRequired";
    public const string ClassroomNameTooLong = "ClassroomNameTooLong";
    public const string ClassroomNameExists = "ClassroomNameExists";
    public const string ClassroomNotFound = "ClassroomNotFound";
    public const string BranchSelectRequired = "BranchSelectRequired";
    public const string GradeLevelRequired = "GradeLevelRequired";
    public const string CalendarTitleRequired = "CalendarTitleRequired";
    public const string CalendarKindInvalid = "CalendarKindInvalid";
    public const string CalendarEndBeforeStart = "CalendarEndBeforeStart";
    public const string CalendarEntryOutsideTerm = "CalendarEntryOutsideTerm";
    public const string CalendarEntryNotFound = "CalendarEntryNotFound";
    public const string TeacherListRequired = "TeacherListRequired";
    public const string TeacherRepeated = "TeacherRepeated";
    public const string TeachersTooMany = "TeachersTooMany";
    public const string TeacherUnavailable = "TeacherUnavailable";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> GetAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string Get(this ILocalizationService localization, string key) =>
        localization.GetAsync(key).GetAwaiter().GetResult();
}
