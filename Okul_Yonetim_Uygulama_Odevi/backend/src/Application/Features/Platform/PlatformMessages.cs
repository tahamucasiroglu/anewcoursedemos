using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Platform;

/// <summary>Platform paneli metin anahtarları (spec 0007); metinler <c>platform.tr.yaml</c>.</summary>
public static class PlatformMessages
{
    public const string Section = "Platform";
    public const string Forbidden = "Forbidden";
    public const string SchoolNotFound = "SchoolNotFound";
    public const string SchoolExists = "SchoolExists";
    public const string SchoolNameRequired = "SchoolNameRequired";
    public const string BranchNameRequired = "BranchNameRequired";
    public const string FullNameRequired = "FullNameRequired";
    public const string NameTooLong = "NameTooLong";
    public const string EmailRequired = "EmailRequired";
    public const string EmailInvalid = "EmailInvalid";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> GetAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string Get(this ILocalizationService localization, string key) =>
        localization.GetAsync(key).GetAwaiter().GetResult();
}
