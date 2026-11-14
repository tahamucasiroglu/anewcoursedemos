namespace TahaKonsept.Modules.Providers.Application.Common;

/// <summary>
/// Kullanıcının girdiği API kök adresini (Yerel LLM Base URL) doğrular ve sadeleştirir: mutlak http/https adresi olmalı,
/// sorgu dizesi olmamalı; sondaki <c>/</c> atılır. Önemli üye: <see cref="Normalize"/>.
/// </summary>
public static class BaseUrlNormalizer
{
    /// <summary>Adresi doğrular ve sadeleştirir.</summary>
    /// <param name="value">Girilen adres.</param>
    /// <returns>Sade adres (ör. <c>http://localhost:11434/v1</c>) ya da geçersizse null.</returns>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().TrimEnd('/');
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && string.IsNullOrEmpty(uri.Query)
            ? trimmed
            : null;
    }
}
