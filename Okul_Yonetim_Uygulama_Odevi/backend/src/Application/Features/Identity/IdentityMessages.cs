using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Identity;

/// <summary>
/// Identity metinlerinin anahtarları ve okuyucusu (YAML: <c>Features/Identity/Resources/Locales/identity.tr.yaml</c>;
/// bölüm adı = özellik klasörü adı). Metin koda gömülmez; doğrulayıcılar eşzamanlı okuyucuyu kullanır.
/// </summary>
public static class IdentityMessages
{
    public const string Section = "Identity";
    public const string InvalidCredentials = "InvalidCredentials";
    public const string AccountLocked = "AccountLocked";
    public const string InvalidSession = "InvalidSession";
    public const string TooManyRequests = "TooManyRequests";
    public const string TenantNotFound = "TenantNotFound";
    public const string EmailRequired = "EmailRequired";
    public const string EmailInvalid = "EmailInvalid";
    public const string PasswordRequired = "PasswordRequired";
    public const string PasswordTooShort = "PasswordTooShort";
    public const string SchoolNameRequired = "SchoolNameRequired";
    public const string BranchNameRequired = "BranchNameRequired";
    public const string FullNameRequired = "FullNameRequired";
    public const string NameTooLong = "NameTooLong";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> GetAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string Get(this ILocalizationService localization, string key) =>
        localization.GetAsync(key).GetAwaiter().GetResult();
}
