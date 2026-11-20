using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Yoklama (spec 0010) metinlerinin anahtarları ve okuyucusu (YAML: <c>Features/Attendance/Resources/Locales/
/// attendance.tr.yaml</c>). Metin koda gömülmez; doğrulayıcılar eşzamanlı okuyucuyu kullanır.
/// </summary>
public static class AttendanceMessages
{
    public const string Section = "Attendance";
    public const string TeacherOnly = "TeacherOnly";
    public const string ClassroomNotFound = "ClassroomNotFound";
    public const string PastDayLocked = "PastDayLocked";
    public const string OnlyToday = "OnlyToday";
    public const string NotSchoolDay = "NotSchoolDay";
    public const string DayRequired = "DayRequired";
    public const string EntriesRequired = "EntriesRequired";
    public const string EntriesTooMany = "EntriesTooMany";
    public const string StudentRepeated = "StudentRepeated";
    public const string StatusInvalid = "StatusInvalid";
    public const string TooManyRequests = "TooManyRequests";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> AttendanceTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string AttendanceText(this ILocalizationService localization, string key) =>
        localization.AttendanceTextAsync(key).GetAwaiter().GetResult();
}
