using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Modules.Providers.Application.Json;

/// <summary>
/// Model metninden JSON çözer: ```json çitlerini ve baştaki/sondaki açıklamayı atar (ilk <c>{</c> ile son <c>}</c> arası),
/// sonra <see cref="ContractJson.Options"/> ile çözer.
/// </summary>
public static class JsonResponseParser
{
    /// <summary>JSON'u çözmeyi dener.</summary>
    /// <typeparam name="T">Hedef tür.</typeparam>
    /// <param name="text">Model metni.</param>
    /// <param name="value">Çözülen değer.</param>
    /// <param name="error">Hata açıklaması (başarısızsa).</param>
    /// <returns>Başarılı mı.</returns>
    public static bool TryParse<T>(string? text, [NotNullWhen(true)] out T? value, out string error)
    {
        value = default;
        var json = Extract(text);
        if (json.Length == 0)
        {
            error = "Yanıtta JSON nesnesi bulunamadı.";
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(json, ContractJson.Options);
            if (value is null)
            {
                error = "JSON boş (null) çözüldü.";
                return false;
            }

            error = string.Empty;
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Metinden JSON nesnesi kısmını ayıklar.</summary>
    /// <param name="text">Model metni.</param>
    /// <returns>JSON metni (bulunamazsa boş).</returns>
    public static string Extract(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var start = text.IndexOf('{', StringComparison.Ordinal);
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : string.Empty;
    }
}
