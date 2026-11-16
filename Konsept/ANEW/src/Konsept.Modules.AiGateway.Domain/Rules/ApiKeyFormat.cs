using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Domain.Rules;

/// <summary>
/// API key biçim kuralı (spec 0004 Req 8): baş/son boşluk kırpılır; boş, 8 karakterden kısa, 512'den uzun ya da kontrol
/// karakteri içeren değer geçersizdir. Sağlayıcı öneki (<c>sk-</c> …) aranmaz.
/// </summary>
public static class ApiKeyFormat
{
    /// <summary>En kısa key uzunluğu.</summary>
    public const int MinLength = 8;

    /// <summary>En uzun key uzunluğu.</summary>
    public const int MaxLength = 512;

    /// <summary>Key'i kırpar.</summary>
    /// <param name="value">Girilen değer.</param>
    /// <returns>Kırpılmış değer (<c>null</c> → boş).</returns>
    public static string Normalize(string? value) => (value ?? string.Empty).Trim();

    /// <summary>Kırpılmış değerin sorununu söyler.</summary>
    /// <param name="value">Girilen değer.</param>
    /// <returns>Sorun; geçerliyse <see cref="ApiKeyFormatIssue.None"/>.</returns>
    public static ApiKeyFormatIssue Check(string? value)
    {
        var key = Normalize(value);
        if (key.Length == 0)
        {
            return ApiKeyFormatIssue.Empty;
        }

        if (key.Any(char.IsControl))
        {
            return ApiKeyFormatIssue.ControlCharacter;
        }

        return key.Length switch
        {
            < MinLength => ApiKeyFormatIssue.TooShort,
            > MaxLength => ApiKeyFormatIssue.TooLong,
            _ => ApiKeyFormatIssue.None,
        };
    }
}
