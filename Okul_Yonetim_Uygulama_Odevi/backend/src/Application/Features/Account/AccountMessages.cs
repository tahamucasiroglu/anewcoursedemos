using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Hesabım ve şifre işlemlerinin Türkçe metin anahtarları (spec 0006); metinler <c>account.tr.yaml</c>.</summary>
public static class AccountMessages
{
    public const string Section = "Account";
    public const string NotSignedIn = "NotSignedIn";
    public const string CurrentPasswordRequired = "CurrentPasswordRequired";
    public const string CurrentPasswordWrong = "CurrentPasswordWrong";
    public const string PasswordRequired = "PasswordRequired";
    public const string PasswordTooShort = "PasswordTooShort";
    public const string PasswordMismatch = "PasswordMismatch";
    public const string PasswordSameAsCurrent = "PasswordSameAsCurrent";
    public const string AccountLocked = "AccountLocked";
    public const string EmailRequired = "EmailRequired";
    public const string EmailInvalid = "EmailInvalid";
    public const string ResetRequested = "ResetRequested";
    public const string ResetLinkInvalid = "ResetLinkInvalid";
    public const string TooManyRequests = "TooManyRequests";
    public const string ResetEmailSubject = "ResetEmailSubject";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> GetAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string Get(this ILocalizationService localization, string key) =>
        localization.GetAsync(key).GetAwaiter().GetResult();
}
