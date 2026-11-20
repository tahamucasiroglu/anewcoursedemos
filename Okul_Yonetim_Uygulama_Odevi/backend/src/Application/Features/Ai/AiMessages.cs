using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Ai;

/// <summary>
/// Ai katmanının (spec 0012) kullanıcı metinlerinin anahtarları ve okuyucusu (YAML:
/// <c>Features/Ai/Resources/Locales/ai.tr.yaml</c>): ses alanı hataları, sesli not süresi, anlaşılmayan ses, "kullanılamıyor",
/// yetki, kapanmış konuşma, bulunamayan ya da kullanılmış taslak.
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

    private static readonly string[] Locales = ["tr"];

    public static Task<string> AiTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string AiText(this ILocalizationService localization, string key) =>
        localization.AiTextAsync(key).GetAwaiter().GetResult();
}
