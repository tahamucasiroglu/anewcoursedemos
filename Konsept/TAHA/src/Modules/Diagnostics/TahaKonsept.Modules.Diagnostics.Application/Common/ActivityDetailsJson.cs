using System.Text.Encodings.Web;
using System.Text.Json;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Modules.Diagnostics.Application.Common;

/// <summary>
/// Günlük ayrıntısını (herhangi bir nesne) geçerli JSON metnine çevirir: <c>null</c> → <c>null</c>; <see cref="JsonElement"/> → ham metin;
/// JSON gibi görünen geçerli metin → olduğu gibi; diğer metin → JSON dizesi; istisna → <c>{ type, message, stackTrace }</c>;
/// diğer nesneler → <see cref="ContractJson"/> kuralları (camelCase, enum metin) + Türkçe karakterler kaçışsız serileştirme (başarısızsa <c>ToString()</c> dizesi).
/// </summary>
public static class ActivityDetailsJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Ayrıntıyı JSON metnine çevirir.</summary>
    /// <param name="details">Ayrıntı.</param>
    /// <returns>JSON metni ya da <c>null</c>.</returns>
    public static string? Serialize(object? details)
    {
        switch (details)
        {
            case null:
                return null;
            case JsonElement { ValueKind: JsonValueKind.Undefined or JsonValueKind.Null }:
                return null;
            case JsonElement element:
                return element.GetRawText();
            case string text:
                return string.IsNullOrWhiteSpace(text) ? null : (IsJson(text) ? text : JsonSerializer.Serialize(text, Options));
            case Exception exception:
                return JsonSerializer.Serialize(
                    new Dictionary<string, string?>
                    {
                        ["type"] = exception.GetType().FullName,
                        ["message"] = exception.Message,
                        ["stackTrace"] = exception.ToString(),
                    },
                    Options);
            default:
                try
                {
                    return JsonSerializer.Serialize(details, details.GetType(), Options);
                }
                catch (NotSupportedException)
                {
                    return JsonSerializer.Serialize(details.ToString(), Options);
                }
                catch (JsonException)
                {
                    return JsonSerializer.Serialize(details.ToString(), Options);
                }
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        // Türkçe karakterler veritabanında okunur kalsın (kaçış dizisi yerine doğrudan karakter); çıktı yalnızca JSON sütununa yazılır, HTML'e değil.
        var options = new JsonSerializerOptions(ContractJson.Options) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        options.MakeReadOnly();
        return options;
    }

    private static bool IsJson(string text)
    {
        var trimmed = text.TrimStart();
        if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '['))
        {
            return false;
        }

        try
        {
            using var _ = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
