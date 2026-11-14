using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;

namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

/// <summary>
/// Görsel/ses sağlayıcılarının ortak HTTP tesisatı: JSON istek kurma (<see cref="Json"/>), gönderip gövdeyi okuma ve HTTP
/// hatasını <see cref="ProviderHttpErrors"/> ile <c>providers.*</c> koduna çevirme (<see cref="SendAsync"/>), sonuç URL'sinden
/// (ya da <c>data:</c> URI'sinden) dosya indirme (<see cref="DownloadAsync"/>) ve hata gövdesinden mesaj çıkarma
/// (<see cref="ExtractErrorMessage"/>; OpenAI/Gemini/fal/ElevenLabs/Replicate/BFL biçimleri).
/// </summary>
public static class MediaHttp
{
    /// <summary>JSON gövdeli istek kurar.</summary>
    /// <param name="method">HTTP yöntemi.</param>
    /// <param name="uri">Adres.</param>
    /// <param name="body">Gövde.</param>
    public static HttpRequestMessage Json(HttpMethod method, Uri uri, JsonNode body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return new HttpRequestMessage(method, uri)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
    }

    /// <summary>İsteği gönderir, gövdeyi okur; başarısız durumda eşlenmiş hata döner. İstek nesnesini serbest bırakır.</summary>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="request">İstek.</param>
    /// <param name="platformName">Görünen platform adı.</param>
    /// <param name="ct">İptal belirteci.</param>
    public static async Task<Result<MediaHttpResponse>> SendAsync(HttpClient http, HttpRequestMessage request, string platformName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(request);
        using (request)
        {
            using var response = await http.SendAsync(request, ct);
            var body = await response.Content.ReadAsByteArrayAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return ProviderHttpErrors.FromStatus(platformName, (int)response.StatusCode, ExtractErrorMessage(Encoding.UTF8.GetString(body)));
            }

            return new MediaHttpResponse(body, response.Content.Headers.ContentType?.MediaType);
        }
    }

    /// <summary>Sonuç dosyasını indirir (kimlik başlığı eklenmez). <c>data:&lt;mime&gt;;base64,…</c> URI'leri yerinde çözülür.</summary>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="url">Dosya adresi ya da data URI.</param>
    /// <param name="platformName">Görünen platform adı.</param>
    /// <param name="ct">İptal belirteci.</param>
    public static async Task<Result<MediaHttpResponse>> DownloadAsync(HttpClient http, string url, string platformName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Error.Failure(ProviderErrorCodes.EmptyResponse, $"{platformName} sonuç adresi vermedi.");
        }

        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return DecodeDataUri(url, platformName);
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Error.Failure(ProviderErrorCodes.BadRequest, $"{platformName} geçersiz sonuç adresi döndü.");
        }

        return await SendAsync(http, new HttpRequestMessage(HttpMethod.Get, uri), platformName, ct);
    }

    /// <summary>Hata gövdesinden okunur mesajı çıkarır; JSON değilse gövdenin kendisi.</summary>
    /// <param name="body">Gövde.</param>
    public static string ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return FindMessage(document.RootElement) ?? body;
        }
        catch (JsonException)
        {
            return body;
        }
    }

    /// <summary>Bilinen hata alanlarını sırayla arar (<c>error.message</c>, <c>error</c>, <c>detail.message</c>, <c>detail</c>, <c>message</c>).</summary>
    /// <param name="root">Kök öğe.</param>
    private static string? FindMessage(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in new[] { "error", "detail", "message", "title" })
        {
            if (!root.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            if (value.ValueKind == JsonValueKind.Object && FindMessage(value) is { } nested)
            {
                return nested;
            }

            if (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0)
            {
                var first = value[0];
                return first.ValueKind == JsonValueKind.Object ? FindMessage(first) ?? first.ToString() : first.ToString();
            }
        }

        return null;
    }

    /// <summary><c>data:</c> URI'sini çözer.</summary>
    /// <param name="url">URI.</param>
    /// <param name="platformName">Görünen platform adı.</param>
    private static Result<MediaHttpResponse> DecodeDataUri(string url, string platformName)
    {
        var comma = url.IndexOf(',', StringComparison.Ordinal);
        var header = comma > 5 ? url[5..comma] : string.Empty;
        if (comma < 0 || !header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Failure(ProviderErrorCodes.BadRequest, $"{platformName} desteklenmeyen data URI döndü.");
        }

        try
        {
            var mime = header[..^";base64".Length];
            return new MediaHttpResponse(Convert.FromBase64String(url[(comma + 1)..]), mime.Length == 0 ? null : mime);
        }
        catch (FormatException ex)
        {
            return Error.Failure(ProviderErrorCodes.BadRequest, $"{platformName} data URI çözülemedi: {ex.Message}");
        }
    }
}
