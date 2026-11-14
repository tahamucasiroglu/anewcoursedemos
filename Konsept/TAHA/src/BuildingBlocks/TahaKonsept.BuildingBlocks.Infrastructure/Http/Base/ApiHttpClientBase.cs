using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Http.Base;

/// <summary>
/// JSON tabanlı HTTP istemci tabanı (Web → API ve ajan → API). Tüm çağrılar istisna yerine <see cref="Result"/> döner;
/// API'nin ProblemDetails gövdesi (<c>code</c>, <c>detail</c>, <c>errors</c>) <see cref="Error"/>'a çevrilir.
/// Genişletme noktaları: <see cref="PrepareRequestAsync"/> (ör. Bearer token), <see cref="TryRecoverUnauthorizedAsync"/>
/// (401'de token yenileyip bir kez tekrar dener), <see cref="JsonOptions"/>.
/// </summary>
public abstract class ApiHttpClientBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="httpClient">Taban adresi ayarlanmış HttpClient (IHttpClientFactory ile).</param>
    protected ApiHttpClientBase(HttpClient httpClient)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>Alttaki HttpClient.</summary>
    protected HttpClient HttpClient { get; }

    /// <summary>JSON ayarları (varsayılan: <see cref="ContractJson.Options"/>).</summary>
    protected virtual JsonSerializerOptions JsonOptions => ContractJson.Options;

    /// <summary>GET isteği.</summary>
    /// <typeparam name="TResponse">Yanıt türü.</typeparam>
    /// <param name="uri">Göreli adres (ör. <c>api/concepts</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result<TResponse>> GetAsync<TResponse>(string uri, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Get, uri, null, cancellationToken);

    /// <summary>POST isteği (yanıt gövdeli).</summary>
    /// <typeparam name="TRequest">İstek türü.</typeparam>
    /// <typeparam name="TResponse">Yanıt türü.</typeparam>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result<TResponse>> PostAsync<TRequest, TResponse>(string uri, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Post, uri, body, cancellationToken);

    /// <summary>POST isteği (yanıt gövdesiz).</summary>
    /// <typeparam name="TRequest">İstek türü.</typeparam>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> PostAsync<TRequest>(string uri, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, uri, body, cancellationToken);

    /// <summary>PUT isteği (yanıt gövdeli).</summary>
    /// <typeparam name="TRequest">İstek türü.</typeparam>
    /// <typeparam name="TResponse">Yanıt türü.</typeparam>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result<TResponse>> PutAsync<TRequest, TResponse>(string uri, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Put, uri, body, cancellationToken);

    /// <summary>PUT isteği (yanıt gövdesiz).</summary>
    /// <typeparam name="TRequest">İstek türü.</typeparam>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> PutAsync<TRequest>(string uri, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, uri, body, cancellationToken);

    /// <summary>DELETE isteği.</summary>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result> DeleteAsync(string uri, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, uri, null, cancellationToken);

    /// <summary>Gövdeli yanıt bekleyen genel gönderim.</summary>
    /// <typeparam name="TResponse">Yanıt türü.</typeparam>
    /// <param name="method">HTTP metodu.</param>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="body">Gövde (opsiyonel).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected virtual async Task<Result<TResponse>> SendAsync<TResponse>(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        var sent = await SendCoreAsync(method, uri, body, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        using var response = sent.Value;
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return Result<TResponse>.Success(default!);
        }

        try
        {
            var value = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
            return Result<TResponse>.Success(value!);
        }
        catch (JsonException ex)
        {
            return Error.Failure("http.invalid_response", "Sunucu yanıtı okunamadı: " + ex.Message);
        }
    }

    /// <summary>Gövdesiz yanıt bekleyen genel gönderim.</summary>
    /// <param name="method">HTTP metodu.</param>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="body">Gövde (opsiyonel).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected virtual async Task<Result> SendAsync(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        var sent = await SendCoreAsync(method, uri, body, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        sent.Value.Dispose();
        return Result.Success();
    }

    /// <summary>İstek gönderilmeden önce çağrılır (ör. <c>Authorization</c> başlığı eklemek için).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected virtual Task PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>401 alındığında çağrılır; <c>true</c> dönerse (ör. token yenilendi) istek bir kez tekrarlanır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Tekrar denensin mi.</returns>
    protected virtual Task<bool> TryRecoverUnauthorizedAsync(CancellationToken cancellationToken) => Task.FromResult(false);

    /// <summary>Başarısız yanıtı <see cref="Error"/>'a çevirir (ProblemDetails okunur).</summary>
    /// <param name="response">Yanıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Hata.</returns>
    protected virtual async Task<Error> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var type = MapStatus(response.StatusCode);
        var code = "http." + status.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var message = response.ReasonPhrase ?? "İstek başarısız.";
        Dictionary<string, string[]>? details = null;

        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetString(root, ProblemExtensionKeys.Code, out var c))
                    {
                        code = c;
                    }

                    if (TryGetString(root, "detail", out var d) || TryGetString(root, "title", out d))
                    {
                        message = d;
                    }

                    if (TryGetString(root, ProblemExtensionKeys.ErrorType, out var t) && Enum.TryParse<ErrorType>(t, true, out var parsed))
                    {
                        type = parsed;
                    }

                    if (root.TryGetProperty(ProblemExtensionKeys.Errors, out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        details = [];
                        foreach (var property in errors.EnumerateObject())
                        {
                            details[property.Name] = property.Value.ValueKind == JsonValueKind.Array
                                ? property.Value.EnumerateArray().Select(e => e.ToString()).ToArray()
                                : [property.Value.ToString()];
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Gövde JSON değil; durum koduna göre hata yeterli.
        }

        return new Error(code, message, type) { Details = details };
    }

    private async Task<Result<HttpResponseMessage>> SendCoreAsync(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendOnceAsync(method, uri, body, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRecoverUnauthorizedAsync(cancellationToken))
            {
                response.Dispose();
                response = await SendOnceAsync(method, uri, body, cancellationToken);
            }

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            using (response)
            {
                return await ReadErrorAsync(response, cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            return Error.Failure("http.unreachable", "Sunucuya ulaşılamadı: " + ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Error.Failure("http.timeout", "Sunucu zamanında yanıt vermedi.");
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        }

        await PrepareRequestAsync(request, cancellationToken);
        return await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static ErrorType MapStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ErrorType.Validation,
        HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
        HttpStatusCode.Forbidden => ErrorType.Forbidden,
        HttpStatusCode.NotFound => ErrorType.NotFound,
        HttpStatusCode.Conflict => ErrorType.Conflict,
        _ => ErrorType.Failure,
    };

    private static bool TryGetString(JsonElement element, string name, out string value)
    {
        if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return value.Length > 0;
        }

        value = string.Empty;
        return false;
    }
}
