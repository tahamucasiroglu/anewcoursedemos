using System.Text.Json;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;

namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

/// <summary>
/// Görsel/ses yanıtlarını okuma yardımcıları: gövdeyi JSON olarak çözüp okuyucuya verir, bozuk JSON / base64 hatalarını
/// <c>providers.bad_request</c>'e çevirir (<see cref="Read{T}"/>); güvenli alan okuyucuları (<see cref="Text"/>, <see cref="Number"/>, <see cref="Items"/>,
/// <see cref="Path"/>).
/// </summary>
public static class MediaJson
{
    /// <summary>Gövdeyi JSON olarak çözer ve okur (belge okuyucudan sonra serbest bırakılır).</summary>
    /// <typeparam name="T">Okunan değer.</typeparam>
    /// <param name="response">Yanıt.</param>
    /// <param name="platformName">Görünen platform adı.</param>
    /// <param name="read">Okuyucu.</param>
    public static Result<T> Read<T>(MediaHttpResponse response, string platformName, Func<JsonElement, Result<T>> read)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(read);
        try
        {
            using var document = JsonDocument.Parse(response.Body);
            return read(document.RootElement);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            return Error.Failure(ProviderErrorCodes.BadRequest, $"{platformName} yanıtı çözülemedi: {ex.Message}");
        }
    }

    /// <summary>Metin alanı (yoksa / metin değilse null).</summary>
    /// <param name="element">Nesne.</param>
    /// <param name="name">Alan adı.</param>
    public static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>Sayı alanı (yoksa 0).</summary>
    /// <param name="element">Nesne.</param>
    /// <param name="name">Alan adı.</param>
    public static long Number(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt64()
            : 0;

    /// <summary>İç içe nesne yolunu izler (ör. <c>Path(root, "urls", "get")</c>).</summary>
    /// <param name="element">Başlangıç.</param>
    /// <param name="names">Alan adları.</param>
    /// <returns>Öğe ya da yol yoksa null.</returns>
    public static JsonElement? Path(JsonElement element, params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var current = element;
        foreach (var name in names)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out current))
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>Dizi alanındaki öğeler (yoksa boş).</summary>
    /// <param name="element">Nesne.</param>
    /// <param name="name">Alan adı.</param>
    public static IEnumerable<JsonElement> Items(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().ToList()
            : [];
}
