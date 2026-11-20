using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration;

/// <summary>
/// Yönetim (spec 0005) metinlerinin anahtarları ve okuyucusu (YAML: <c>Features/Administration/Resources/Locales/
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
    public const string TooManyRequests = "TooManyRequests";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> GetAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string Get(this ILocalizationService localization, string key) =>
        localization.GetAsync(key).GetAwaiter().GetResult();
}
