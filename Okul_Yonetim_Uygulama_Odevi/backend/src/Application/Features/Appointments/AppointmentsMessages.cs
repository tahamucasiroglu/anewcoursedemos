using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Appointments;

/// <summary>
/// Randevu (spec 0016) metinlerinin anahtarları ve okuyucusu (YAML: <c>Features/Appointments/Resources/Locales/appointments.tr.yaml</c>).
/// Metin koda gömülmez; doğrulayıcılar eşzamanlı okuyucuyu kullanır. Bildirim metinleri <c>{teacher}</c>, <c>{day}</c> (gün.ay.yıl),
/// <c>{time}</c> (SS:DD) ve <c>{reason}</c> yer tutucularını taşır; tek geçişte doldurulur (<see cref="AppointmentNoticeText"/>).
/// </summary>
public static class AppointmentsMessages
{
    public const string Section = "Appointments";
    public const string TeacherOnly = "TeacherOnly";
    public const string GuardianOnly = "GuardianOnly";
    public const string Forbidden = "Forbidden";
    public const string RequesterOnly = "RequesterOnly";
    public const string ChildNotFound = "ChildNotFound";
    public const string TeacherNotFound = "TeacherNotFound";
    public const string SlotNotFound = "SlotNotFound";
    public const string AppointmentNotFound = "AppointmentNotFound";
    public const string DayRequired = "DayRequired";
    public const string DayOutOfRange = "DayOutOfRange";
    public const string StartInvalid = "StartInvalid";
    public const string EndInvalid = "EndInvalid";
    public const string StartBeforeEnd = "StartBeforeEnd";
    public const string RangeTooLong = "RangeTooLong";
    public const string DurationInvalid = "DurationInvalid";
    public const string NoSlotsLeft = "NoSlotsLeft";
    public const string Overlap = "Overlap";
    public const string SlotHasRequest = "SlotHasRequest";
    public const string SlotStartedRemove = "SlotStartedRemove";
    public const string SlotRequired = "SlotRequired";
    public const string StudentRequired = "StudentRequired";
    public const string TopicRequired = "TopicRequired";
    public const string TopicTooLong = "TopicTooLong";
    public const string ReasonTooLong = "ReasonTooLong";
    public const string DecisionInvalid = "DecisionInvalid";
    public const string SlotTaken = "SlotTaken";
    public const string SlotStarted = "SlotStarted";
    public const string OpenAppointmentExists = "OpenAppointmentExists";
    public const string AlreadyResponded = "AlreadyResponded";
    public const string RequestExpired = "RequestExpired";
    public const string StartedCannotCancel = "StartedCannotCancel";
    public const string CannotCancel = "CannotCancel";
    public const string RejectInsteadOfCancel = "RejectInsteadOfCancel";
    public const string TooManyRequests = "TooManyRequests";
    public const string ApprovedNotice = "ApprovedNotice";
    public const string RejectedNotice = "RejectedNotice";
    public const string RejectedWithReasonNotice = "RejectedWithReasonNotice";
    public const string TeacherCancelledNotice = "TeacherCancelledNotice";
    public const string TeacherCancelledWithReasonNotice = "TeacherCancelledWithReasonNotice";
    public const string NewRequestNotice = "NewRequestNotice";
    public const string GuardianCancelledNotice = "GuardianCancelledNotice";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> AppointmentsTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string AppointmentsText(this ILocalizationService localization, string key) =>
        localization.AppointmentsTextAsync(key).GetAwaiter().GetResult();
}
