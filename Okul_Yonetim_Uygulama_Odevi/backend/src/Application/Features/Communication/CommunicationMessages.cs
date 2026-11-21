using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Veliye giden iletişim (spec 0010: devamsızlık bildirimi; spec 0011: duyuru, onay formu, mesaj, mesai saatleri) metinlerinin anahtarları ve okuyucusu (YAML:
/// <c>Features/Communication/Resources/Locales/communication.tr.yaml</c>). Bildirim ve e-posta şablonları
/// <c>{student}</c>, <c>{date}</c>, <c>{school}</c> yer tutucularını taşır (<see cref="GuardianNoticeEmail"/>). Spec 0014: haftalık
/// özet bildiriminin başlığı (<c>{week}</c>), etiketi, "velisi yok" ve metin alanı hataları, bildirim ayrıntısında "bulunamadı".
/// </summary>
public static class CommunicationMessages
{
    public const string Section = "Communication";
    public const string AbsentText = "AbsentText";
    public const string CorrectedText = "CorrectedText";
    public const string AbsentSubject = "AbsentSubject";
    public const string CorrectedSubject = "CorrectedSubject";
    public const string EmailFooter = "EmailFooter";
    public const string ShownNoticesRequired = "ShownNoticesRequired";
    public const string Forbidden = "Forbidden";
    public const string TeacherScopeOnly = "TeacherScopeOnly";
    public const string AnnouncementNotFound = "AnnouncementNotFound";
    public const string ScopeNotFound = "ScopeNotFound";
    public const string NoRecipients = "NoRecipients";
    public const string TitleRequired = "TitleRequired";
    public const string TitleTooLong = "TitleTooLong";
    public const string BodyRequired = "BodyRequired";
    public const string BodyTooLong = "BodyTooLong";
    public const string ScopeInvalid = "ScopeInvalid";
    public const string ScopeIdRequired = "ScopeIdRequired";
    public const string QuestionRequired = "QuestionRequired";
    public const string QuestionTooLong = "QuestionTooLong";
    public const string DeadlineRequired = "DeadlineRequired";
    public const string DeadlineRange = "DeadlineRange";
    public const string StudentRequired = "StudentRequired";
    public const string AnswerInvalid = "AnswerInvalid";
    public const string ConsentNotRequested = "ConsentNotRequested";
    public const string ConsentExpired = "ConsentExpired";
    public const string StudentNotFound = "StudentNotFound";
    public const string ConversationNotFound = "ConversationNotFound";
    public const string ContactNotFound = "ContactNotFound";
    public const string CounterpartRequired = "CounterpartRequired";
    public const string CannotSend = "CannotSend";
    public const string MessageRequired = "MessageRequired";
    public const string MessageTooLong = "MessageTooLong";
    public const string StartInvalid = "StartInvalid";
    public const string EndInvalid = "EndInvalid";
    public const string StartBeforeEnd = "StartBeforeEnd";
    public const string DaysRequired = "DaysRequired";
    public const string DaysInvalid = "DaysInvalid";
    public const string TooManyRequests = "TooManyRequests";
    public const string PageRange = "PageRange";
    public const string WeeklySummaryTitle = "WeeklySummaryTitle";
    public const string WeeklySummaryLabel = "WeeklySummaryLabel";
    public const string WeeklySummaryNoGuardian = "WeeklySummaryNoGuardian";
    public const string WeeklySummaryTextRequired = "WeeklySummaryTextRequired";
    public const string WeeklySummaryTextTooLong = "WeeklySummaryTextTooLong";
    public const string NoticeNotFound = "NoticeNotFound";
    public const string WeeklySummaryWeekInvalid = "WeeklySummaryWeekInvalid";
    public const string WeeklySummaryBusy = "WeeklySummaryBusy";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> CommunicationTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string CommunicationText(this ILocalizationService localization, string key) =>
        localization.CommunicationTextAsync(key).GetAwaiter().GetResult();
}
