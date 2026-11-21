using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Academics;

/// <summary>
/// Ödev ve not (spec 0013) metinlerinin anahtarları ve okuyucusu (YAML: <c>Features/Academics/Resources/Locales/academics.tr.yaml</c>).
/// Metin koda gömülmez; doğrulayıcılar eşzamanlı okuyucuyu kullanır. Yeni ödev bildirimi <c>{subject}</c>, <c>{title}</c>,
/// <c>{due}</c> (gün.ay.yıl) yer tutucularını taşır.
/// </summary>
public static class AcademicsMessages
{
    public const string Section = "Academics";
    public const string TeacherOnly = "TeacherOnly";
    public const string ClassroomNotFound = "ClassroomNotFound";
    public const string AssignmentNotFound = "AssignmentNotFound";
    public const string AssessmentNotFound = "AssessmentNotFound";
    public const string OwnerOnly = "OwnerOnly";
    public const string ChildNotFound = "ChildNotFound";
    public const string SubjectRequired = "SubjectRequired";
    public const string SubjectTooLong = "SubjectTooLong";
    public const string AssignmentTitleRequired = "AssignmentTitleRequired";
    public const string AssignmentTitleTooLong = "AssignmentTitleTooLong";
    public const string DescriptionTooLong = "DescriptionTooLong";
    public const string DueDateRequired = "DueDateRequired";
    public const string DueDateOutOfRange = "DueDateOutOfRange";
    public const string AssessmentTitleRequired = "AssessmentTitleRequired";
    public const string AssessmentTitleTooLong = "AssessmentTitleTooLong";
    public const string KindInvalid = "KindInvalid";
    public const string DateRequired = "DateRequired";
    public const string DateInFuture = "DateInFuture";
    public const string DateOutsideTerm = "DateOutsideTerm";
    public const string EntriesRequired = "EntriesRequired";
    public const string EntriesTooMany = "EntriesTooMany";
    public const string StudentRepeated = "StudentRepeated";
    public const string StatusInvalid = "StatusInvalid";
    public const string ScoreInvalid = "ScoreInvalid";
    public const string StudentNotInRecord = "StudentNotInRecord";
    public const string TooManyRequests = "TooManyRequests";
    public const string NewAssignmentNotice = "NewAssignmentNotice";

    /// <summary>Bir kayıttaki (teslim ya da puan listesi) en fazla öğrenci sayısı.</summary>
    public const int MaxEntries = 500;

    private static readonly string[] Locales = ["tr"];

    public static Task<string> AcademicsTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string AcademicsText(this ILocalizationService localization, string key) =>
        localization.AcademicsTextAsync(key).GetAwaiter().GetResult();
}
