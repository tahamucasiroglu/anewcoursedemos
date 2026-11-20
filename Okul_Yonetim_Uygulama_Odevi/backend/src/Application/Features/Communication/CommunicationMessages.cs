using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Veliye giden iletişim (spec 0010: devamsızlık bildirimi) metinlerinin anahtarları ve okuyucusu (YAML:
/// <c>Features/Communication/Resources/Locales/communication.tr.yaml</c>). Bildirim ve e-posta şablonları
/// <c>{student}</c>, <c>{date}</c>, <c>{school}</c> yer tutucularını taşır (<see cref="GuardianNoticeEmail"/>).
/// </summary>
public static class CommunicationMessages
{
    public const string Section = "Communication";
    public const string AbsentText = "AbsentText";
    public const string CorrectedText = "CorrectedText";
    public const string AbsentSubject = "AbsentSubject";
    public const string CorrectedSubject = "CorrectedSubject";
    public const string EmailFooter = "EmailFooter";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> CommunicationTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }
}
