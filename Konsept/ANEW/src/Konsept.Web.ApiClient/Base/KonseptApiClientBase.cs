using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Shared.Kernel.Security;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;

namespace Konsept.Web.ApiClient.Base;

/// <summary>
/// API çağrılarının ortak HTTP mantığı: başarıda JSON gövdesini okur; ProblemDetails'i <see cref="Error"/>'a çevirir (kod,
/// başlık, alan hataları, kalan hak / kilit / korelasyon ek bilgisi); ağ hatası ve zaman aşımı
/// <see cref="ApiClientErrors.Unavailable"/> olur.
/// </summary>
/// <param name="options">İstemci ayarları (servis anahtarı).</param>
public abstract class KonseptApiClientBase(ApiClientOptions options) : IKonseptApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public Task<Result<SystemStatusResponse>> GetSystemStatusAsync(CancellationToken cancellationToken) =>
        SendAsync<SystemStatusResponse>(new HttpRequestMessage(HttpMethod.Get, "api/system/status"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<AgentTestResponse>> TestAgentAsync(CancellationToken cancellationToken) =>
        SendAsync<AgentTestResponse>(new HttpRequestMessage(HttpMethod.Post, "api/agent/test"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<int>> ForwardLogsAsync(IReadOnlyList<ForwardedLogRequest> logs, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/internal/logs") { Content = JsonContent.Create(logs, options: Json) };
        AddServiceKey(request);
        return SendAsync<int>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<AuthSessionResponse>> RegisterAsync(RegisterApiRequest request, string? clientIp, CancellationToken cancellationToken) =>
        SendAsync<AuthSessionResponse>(WithClientIp(new HttpRequestMessage(HttpMethod.Post, "api/auth/register") { Content = JsonContent.Create(request, options: Json) }, clientIp), cancellationToken);

    /// <inheritdoc />
    public Task<Result<AuthSessionResponse>> LoginAsync(LoginApiRequest request, string? clientIp, CancellationToken cancellationToken) =>
        SendAsync<AuthSessionResponse>(WithClientIp(new HttpRequestMessage(HttpMethod.Post, "api/auth/login") { Content = JsonContent.Create(request, options: Json) }, clientIp), cancellationToken);

    /// <inheritdoc />
    public Task<Result<AuthSessionResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        SendAsync<AuthSessionResponse>(
            new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh") { Content = JsonContent.Create(new { refreshToken }, options: Json) },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<int>> LogoutAsync(string accessToken, CancellationToken cancellationToken) =>
        SendAsync<int>(WithBearer(new HttpRequestMessage(HttpMethod.Post, "api/auth/logout"), accessToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<CurrentUserResponse>> GetSessionAsync(string accessToken, CancellationToken cancellationToken) =>
        SendAsync<CurrentUserResponse>(WithBearer(new HttpRequestMessage(HttpMethod.Get, "api/auth/session"), accessToken), cancellationToken);

    /// <summary>İsteği API'ye gönderir.</summary>
    /// <param name="request">HTTP isteği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>HTTP cevabı.</returns>
    protected abstract Task<HttpResponseMessage> TransmitAsync(HttpRequestMessage request, CancellationToken cancellationToken);

    private static HttpRequestMessage WithBearer(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static ErrorType TypeOf(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => ErrorType.Validation,
        HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
        HttpStatusCode.NotFound => ErrorType.NotFound,
        HttpStatusCode.Conflict => ErrorType.Conflict,
        HttpStatusCode.TooManyRequests => ErrorType.TooManyRequests,
        HttpStatusCode.GatewayTimeout => ErrorType.Timeout,
        _ => ErrorType.Unavailable,
    };

    private static async Task<Error> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var type = TypeOf(response.StatusCode);
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json, cancellationToken).ConfigureAwait(false);
            var title = Text(problem, "title");
            var fieldErrors = ReadFieldErrors(problem);
            var code = Text(problem, "code") ?? (fieldErrors is null ? "api.error" : ApiClientErrors.ValidationCode);
            return new Error(code, title ?? ApiClientErrors.Unavailable.Message, type)
            {
                FieldErrors = fieldErrors,
                Metadata = ReadMetadata(problem),
            };
        }
        catch (JsonException)
        {
            return type is ErrorType.Unauthorized or ErrorType.TooManyRequests
                ? new Error("api.error", ApiClientErrors.Unavailable.Message, type)
                : ApiClientErrors.Unavailable;
        }
    }

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static Dictionary<string, string[]>? ReadFieldErrors(JsonElement problem)
    {
        if (problem.ValueKind != JsonValueKind.Object || !problem.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return errors.EnumerateObject().ToDictionary(
            p => p.Name,
            p => p.Value.ValueKind == JsonValueKind.Array ? [.. p.Value.EnumerateArray().Select(m => m.GetString() ?? string.Empty)] : Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object>? ReadMetadata(JsonElement problem)
    {
        if (problem.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var metadata = new Dictionary<string, object>(StringComparer.Ordinal);
        if (problem.TryGetProperty(ApiClientErrors.RemainingAttemptsKey, out var remaining) && remaining.TryGetInt32(out var count))
        {
            metadata[ApiClientErrors.RemainingAttemptsKey] = count;
        }

        if (problem.TryGetProperty(ApiClientErrors.LockedUntilKey, out var until) && until.TryGetDateTimeOffset(out var lockedUntil))
        {
            metadata[ApiClientErrors.LockedUntilKey] = lockedUntil;
        }

        if (Text(problem, ApiClientErrors.CorrelationIdKey) is { } correlation)
        {
            metadata[ApiClientErrors.CorrelationIdKey] = correlation;
        }

        return metadata.Count == 0 ? null : metadata;
    }

    private void AddServiceKey(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(options?.ServiceKey))
        {
            request.Headers.Add(ServiceKeyHeader.Name, options.ServiceKey);
        }
    }

    private HttpRequestMessage WithClientIp(HttpRequestMessage request, string? clientIp)
    {
        if (!string.IsNullOrEmpty(clientIp) && !string.IsNullOrEmpty(options?.ServiceKey))
        {
            AddServiceKey(request);
            request.Headers.Add(ClientIpHeader.Name, clientIp);
        }

        return request;
    }

    private async Task<Result<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            try
            {
                using var response = await TransmitAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken).ConfigureAwait(false);
                    return body is null ? Result<T>.Failure(ApiClientErrors.Unavailable) : Result<T>.Success(body);
                }

                return Result<T>.Failure(await ReadProblemAsync(response, cancellationToken).ConfigureAwait(false));
            }
            catch (HttpRequestException)
            {
                return Result<T>.Failure(ApiClientErrors.Unavailable);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result<T>.Failure(ApiClientErrors.Unavailable);
            }
        }
    }
}
