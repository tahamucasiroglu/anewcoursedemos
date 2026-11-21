using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Ai;

/// <summary>
/// Ai katmanının (spec 0012) kullanıcı metinlerinin anahtarları ve okuyucusu (YAML:
/// <c>Features/Ai/Resources/Locales/ai.tr.yaml</c>): ses alanı hataları, sesli not süresi, anlaşılmayan ses, "kullanılamıyor",
/// yetki, kapanmış konuşma, bulunamayan, kullanılmış ya da o an işlenen taslak. Spec 0014: haftalık özetin yetki, hafta, öğretmen
/// notu, "veri yok", "zaten gönderildi", "kullanılamıyor — daha sonra tekrar deneyin", "taslak değişti" (review R1-2) metinleri ve dayanak satırının parçaları.
/// </summary>
public static class AiMessages
{
    public const string Section = "Ai";
    public const string ConversationRequired = "ConversationRequired";
    public const string AudioRequired = "AudioRequired";
    public const string AudioTooLarge = "AudioTooLarge";
    public const string AudioTypeInvalid = "AudioTypeInvalid";
    public const string TooLong = "TooLong";
    public const string NotUnderstood = "NotUnderstood";
    public const string Unavailable = "Unavailable";
    public const string Forbidden = "Forbidden";
    public const string ConversationClosed = "ConversationClosed";
    public const string DraftNotFound = "DraftNotFound";
    public const string DraftUsed = "DraftUsed";
    public const string DraftBusy = "DraftBusy";
    public const string SummaryTeacherOnly = "SummaryTeacherOnly";
    public const string SummaryClassroomNotFound = "SummaryClassroomNotFound";
    public const string SummaryStudentNotFound = "SummaryStudentNotFound";
    public const string SummaryWeekInvalid = "SummaryWeekInvalid";
    public const string SummaryNoteTooLong = "SummaryNoteTooLong";
    public const string SummaryNoData = "SummaryNoData";
    public const string SummaryAlreadySent = "SummaryAlreadySent";
    public const string SummaryUnavailable = "SummaryUnavailable";
    public const string SummaryBusy = "SummaryBusy";
    public const string SummaryDraftChanged = "SummaryDraftChanged";
    public const string BasisPrefix = "BasisPrefix";
    public const string BasisAttendance = "BasisAttendance";
    public const string BasisLate = "BasisLate";
    public const string BasisNoAttendance = "BasisNoAttendance";
    public const string BasisAssignments = "BasisAssignments";
    public const string BasisGrades = "BasisGrades";
    public const string BasisTeacherNote = "BasisTeacherNote";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> AiTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string AiText(this ILocalizationService localization, string key) =>
        localization.AiTextAsync(key).GetAwaiter().GetResult();
}
