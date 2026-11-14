using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TahaKonsept.Shared.Contracts.Identity;
using TahaKonsept.Web.Infrastructure.Api.Abstract;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;

namespace TahaKonsept.Web.Infrastructure.Api.Base;

/// <summary>
/// <see cref="IApiClient"/> ortak davranışı: tek bir generic <see cref="SendAsync{T}"/> akışı (token ekle → gönder →
/// 401'de yenile ve bir kez tekrarla → cevabı <see cref="ApiCallResult{T}"/>'ye çevir). Somut sınıflar yalnızca
/// HttpClient'ı ve gerekirse ek başlıkları sağlar. Ağ hataları 503 sonucu olarak döner, istisna dışarı sızmaz.
/// </summary>
public abstract class ApiClientBase : IApiClient
{
    /// <summary>API ile aynı JSON kuralları (camelCase, büyük/küçük harf duyarsız).</summary>
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>API'ye bağlı HttpClient (BaseAddress ayarlı).</summary>
    protected HttpClient Http { get; }

    /// <summary>Oturumdaki token'ların okunup yazıldığı depo.</summary>
    protected ITokenStore Tokens { get; }

    /// <summary>Token yenileyici.</summary>
    protected ITokenRefresher Refresher { get; }

    /// <summary>Saat kaynağı (token bitişi karşılaştırması için).</summary>
    protected TimeProvider Clock { get; }

    /// <summary>Günlükleyici.</summary>
    protected ILogger Logger { get; }

    /// <summary>Erişim token'ının bitişine kaç saniye kala önceden yenileneceği.</summary>
    protected abstract TimeSpan RefreshSkew { get; }

    /// <summary>Temel bağımlılıkları alır.</summary>
    /// <param name="http">API HttpClient'ı.</param>
    /// <param name="tokens">Token deposu.</param>
    /// <param name="refresher">Token yenileyici.</param>
    /// <param name="clock">Saat kaynağı.</param>
    /// <param name="logger">Günlükleyici.</param>
    protected ApiClientBase(HttpClient http, ITokenStore tokens, ITokenRefresher refresher, TimeProvider clock, ILogger logger)
    {
        Http = http;
        Tokens = tokens;
        Refresher = refresher;
        Clock = clock;
        Logger = logger;
    }

    /// <inheritdoc />
    public Task<ApiCallResult<T>> GetAsync<T>(string url, CancellationToken ct = default) =>
        SendAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, url), ct);

    /// <inheritdoc />
    public Task<ApiCallResult<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default) =>
        SendAsync<TResponse>(() => WithBody(HttpMethod.Post, url, body), ct);

    /// <inheritdoc />
    public Task<ApiCallResult<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default) =>
        SendAsync<TResponse>(() => WithBody(HttpMethod.Put, url, body), ct);

    /// <inheritdoc />
    public Task<ApiCallResult<TResponse>> DeleteAsync<TResponse>(string url, CancellationToken ct = default) =>
        SendAsync<TResponse>(() => new HttpRequestMessage(HttpMethod.Delete, url), ct);

    /// <summary>
    /// Ortak gönderim akışı. İstek her denemede <paramref name="requestFactory"/> ile yeniden kurulur (HttpRequestMessage
    /// tekrar gönderilemez).
    /// </summary>
    /// <typeparam name="T">Cevap türü.</typeparam>
    /// <param name="requestFactory">İsteği oluşturan fonksiyon.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected virtual async Task<ApiCallResult<T>> SendAsync<T>(Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        try
        {
            var tokens = await Tokens.GetAsync();
            if (tokens is not null && tokens.AccessTokenExpiresAtUtc <= Clock.GetUtcNow().Add(RefreshSkew))
            {
                tokens = await Refresher.RefreshAsync(tokens, ct) ?? tokens;
            }

            var response = await SendOnceAsync(requestFactory, tokens, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized && tokens is not null)
            {
                var refreshed = await Refresher.RefreshAsync(tokens, ct);
                if (refreshed is not null)
                {
                    response.Dispose();
                    response = await SendOnceAsync(requestFactory, refreshed, ct);
                }
            }

            using (response)
            {
                return await ReadAsync<T>(response, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ApiCallResult<T>.Failure(499, "İstek iptal edildi.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Logger.LogWarning("API'ye ulaşılamadı ({Url}): {Reason}", Http.BaseAddress, ex.Message);
            return ApiCallResult<T>.Failure(StatusCodes.Status503ServiceUnavailable, "API'ye ulaşılamadı. Sunucu çalışıyor mu?");
        }
    }

    /// <summary>İsteği kurar, varsa Bearer token ekler ve gönderir.</summary>
    /// <param name="requestFactory">İsteği oluşturan fonksiyon.</param>
    /// <param name="tokens">Kullanılacak token'lar (anonim istekte null).</param>
    /// <param name="ct">İptal belirteci.</param>
    protected virtual Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> requestFactory, TokenResponse? tokens, CancellationToken ct)
    {
        var request = requestFactory();
        if (tokens is not null && !string.IsNullOrEmpty(tokens.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return Http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
    }

    /// <summary>HTTP cevabını <see cref="ApiCallResult{T}"/>'ye çevirir.</summary>
    /// <typeparam name="T">Cevap türü.</typeparam>
    /// <param name="response">HTTP cevabı.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected virtual async Task<ApiCallResult<T>> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var status = (int)response.StatusCode;
        var text = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return ApiCallResult<T>.Success(default, status);
            }

            try
            {
                return ApiCallResult<T>.Success(JsonSerializer.Deserialize<T>(text, JsonOptions), status);
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex, "API cevabı {Type} türüne çevrilemedi ({Status}).", typeof(T).Name, status);
                return ApiCallResult<T>.Failure(StatusCodes.Status502BadGateway, "API beklenmeyen bir cevap döndürdü.");
            }
        }

        if (status >= 500)
        {
            Logger.LogError("API hata döndürdü: {Method} {Url} → {Status}", response.RequestMessage?.Method, response.RequestMessage?.RequestUri, status);
        }
        else
        {
            Logger.LogInformation("API isteği reddedildi: {Method} {Url} → {Status}", response.RequestMessage?.Method, response.RequestMessage?.RequestUri, status);
        }

        return ApiCallResult<T>.Failure(status, DefaultMessage(response.StatusCode), text, response.Content.Headers.ContentType?.ToString());
    }

    /// <summary>JSON gövdeli istek oluşturur.</summary>
    /// <typeparam name="TBody">Gövde türü.</typeparam>
    /// <param name="method">HTTP metodu.</param>
    /// <param name="url">Göreli adres.</param>
    /// <param name="body">Gövde.</param>
    protected static HttpRequestMessage WithBody<TBody>(HttpMethod method, string url, TBody body) =>
        new(method, url) { Content = JsonContent.Create(body, options: JsonOptions) };

    /// <summary>Durum koduna göre Türkçe varsayılan mesaj.</summary>
    /// <param name="code">HTTP durum kodu.</param>
    protected static string DefaultMessage(HttpStatusCode code) => code switch
    {
        HttpStatusCode.BadRequest => "Gönderilen bilgiler geçersiz.",
        HttpStatusCode.Unauthorized => "Oturum süresi doldu; lütfen yeniden giriş yapın.",
        HttpStatusCode.Forbidden => "Bu işlem için yetkiniz yok.",
        HttpStatusCode.NotFound => "Kayıt bulunamadı.",
        HttpStatusCode.Conflict => "Kayıt çakışması.",
        HttpStatusCode.TooManyRequests => "Çok fazla istek; biraz bekleyin.",
        _ => "İşlem başarısız oldu.",
    };
}
