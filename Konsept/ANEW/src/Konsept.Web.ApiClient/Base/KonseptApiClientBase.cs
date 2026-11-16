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

    /// <inheritdoc />
    public Task<Result<AdminOverviewResponse>> GetAdminOverviewAsync(CancellationToken cancellationToken) =>
        SendAsync<AdminOverviewResponse>(new HttpRequestMessage(HttpMethod.Get, "api/admin/overview"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<PairingDeviceResponse>> AgentPairingAsync(string action, string? code, string? clientIp, CancellationToken cancellationToken)
    {
        // Kod yalnızca gövdede gider (adreste olsaydı Api istek loguna düz yazılırdı, review r1 M-2).
        var path = action is "lookup" or "approve" or "reject"
            ? "api/agent-pairing/" + action
            : throw new ArgumentOutOfRangeException(nameof(action), action, "Bilinmeyen eşleştirme işlemi.");
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(new { code = code?.Trim() }, options: Json) };
        return SendAsync<PairingDeviceResponse>(WithClientIp(request, clientIp), cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<CatalogResponse>> GetCatalogAsync(CancellationToken cancellationToken) =>
        SendAsync<CatalogResponse>(new HttpRequestMessage(HttpMethod.Get, "api/ai/catalog"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ApiKeyStatusResponse>>> GetApiKeysAsync(CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<ApiKeyStatusResponse>>(new HttpRequestMessage(HttpMethod.Get, "api/ai/keys"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ApiKeyStatusResponse>> SaveApiKeyAsync(string provider, string? apiKey, CancellationToken cancellationToken) =>
        SendAsync<ApiKeyStatusResponse>(
            new HttpRequestMessage(HttpMethod.Put, "api/ai/keys/" + Uri.EscapeDataString(provider ?? string.Empty)) { Content = JsonContent.Create(new { apiKey }, options: Json) },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<MonthlySpendResponse>> GetMonthlySpendAsync(CancellationToken cancellationToken) =>
        SendAsync<MonthlySpendResponse>(new HttpRequestMessage(HttpMethod.Get, "api/ai/spend/month"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<TrialCallResponse>> TrialCallAsync(string? modelId, CancellationToken cancellationToken) =>
        SendAsync<TrialCallResponse>(
            new HttpRequestMessage(HttpMethod.Post, "api/ai/calls/trial") { Content = JsonContent.Create(new { modelId }, options: Json) },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<ApiKeyStatusResponse>> DeleteApiKeyAsync(string provider, CancellationToken cancellationToken) =>
        SendAsync<ApiKeyStatusResponse>(new HttpRequestMessage(HttpMethod.Delete, "api/ai/keys/" + Uri.EscapeDataString(provider ?? string.Empty)), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ProfileListResponse>> GetProfilesAsync(CancellationToken cancellationToken) =>
        SendAsync<ProfileListResponse>(new HttpRequestMessage(HttpMethod.Get, "api/profiles"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ProfileResponse>> CreateProfileAsync(SaveProfileApiRequest? request, CancellationToken cancellationToken) =>
        SendAsync<ProfileResponse>(new HttpRequestMessage(HttpMethod.Post, "api/profiles") { Content = JsonContent.Create(request, options: Json) }, cancellationToken);

    /// <inheritdoc />
    public Task<Result<ProfileResponse>> UpdateProfileAsync(Guid id, SaveProfileApiRequest? request, CancellationToken cancellationToken) =>
        SendAsync<ProfileResponse>(new HttpRequestMessage(HttpMethod.Put, "api/profiles/" + id.ToString("D")) { Content = JsonContent.Create(request, options: Json) }, cancellationToken);

    /// <inheritdoc />
    public Task<Result<ProfileListResponse>> DeleteProfileAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<ProfileListResponse>(new HttpRequestMessage(HttpMethod.Delete, "api/profiles/" + id.ToString("D")), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ProfileResponse>> SelectProfileAsync(Guid? id, CancellationToken cancellationToken) =>
        SendAsync<ProfileResponse>(new HttpRequestMessage(HttpMethod.Put, "api/profiles/selected") { Content = JsonContent.Create(new { profileId = id }, options: Json) }, cancellationToken);

    /// <inheritdoc />
    public Task<Result<ConceptSettingsResponse>> GetConceptSettingsAsync(Guid profileId, CancellationToken cancellationToken) =>
        SendAsync<ConceptSettingsResponse>(new HttpRequestMessage(HttpMethod.Get, $"api/profiles/{profileId:D}/concept-settings"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ConceptSettingsResponse>> SaveConceptSettingsAsync(Guid profileId, SaveConceptSettingsApiRequest? request, CancellationToken cancellationToken) =>
        SendAsync<ConceptSettingsResponse>(
            new HttpRequestMessage(HttpMethod.Put, $"api/profiles/{profileId:D}/concept-settings") { Content = JsonContent.Create(request, options: Json) },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<JsonElement>> GetConceptWorkAsync(Guid profileId, CancellationToken cancellationToken) =>
        SendAsync<JsonElement>(new HttpRequestMessage(HttpMethod.Get, $"api/concepts/work?profileId={profileId:D}"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<JsonElement>> SendConceptCommandAsync(ConceptCommand command, JsonElement? body, CancellationToken cancellationToken)
    {
        var path = command switch
        {
            ConceptCommand.Topics => "api/concepts/topics",
            ConceptCommand.FromTopics => "api/concepts/from-topics",
            ConceptCommand.FromIdea => "api/concepts/from-idea",
            ConceptCommand.Stop => "api/concepts/jobs/stop",
            ConceptCommand.Approve => "api/concepts/approve",
            ConceptCommand.ResetSuggestions => "api/concepts/suggestions/reset",
            _ => "api/concepts/unapprove",
        };
        // Yalnızca JSON nesnesi iletilir; başka her şey boş nesne olur (Api doğrular, Web yorumlamaz).
        HttpContent content = body is { ValueKind: JsonValueKind.Object } json ? JsonContent.Create(json, options: Json) : JsonContent.Create(new { }, options: Json);
        return SendAsync<JsonElement>(new HttpRequestMessage(HttpMethod.Post, path) { Content = content }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<JsonElement>> ListConceptsAsync(Guid profileId, string? statuses, int page, CancellationToken cancellationToken) =>
        SendAsync<JsonElement>(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/concepts?profileId={profileId:D}&statuses={Uri.EscapeDataString(statuses ?? string.Empty)}&page={Math.Max(1, page).ToString(System.Globalization.CultureInfo.InvariantCulture)}"),
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<JsonElement>> GetConceptAsync(Guid conceptId, CancellationToken cancellationToken) =>
        SendAsync<JsonElement>(new HttpRequestMessage(HttpMethod.Get, $"api/concepts/{conceptId:D}"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<JsonElement>> SendConceptEditAsync(Guid conceptId, ConceptEditCommand command, JsonElement? body, CancellationToken cancellationToken)
    {
        var path = command switch
        {
            ConceptEditCommand.SaveScenes => $"api/concepts/{conceptId:D}/scenes",
            ConceptEditCommand.Regenerate => $"api/concepts/{conceptId:D}/regenerate",
            ConceptEditCommand.ApproveRevision => $"api/concepts/{conceptId:D}/revision/approve",
            ConceptEditCommand.Delete => $"api/concepts/{conceptId:D}/delete",
            ConceptEditCommand.Purge => $"api/concepts/deleted/{conceptId:D}/purge",
            _ => $"api/concepts/{conceptId:D}/revision/discard",
        };
        HttpContent content = body is { ValueKind: JsonValueKind.Object } json ? JsonContent.Create(json, options: Json) : JsonContent.Create(new { }, options: Json);
        return SendAsync<JsonElement>(new HttpRequestMessage(HttpMethod.Post, path) { Content = content }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<JsonElement>> GetConceptRevisionAsync(Guid conceptId, CancellationToken cancellationToken) =>
        SendAsync<JsonElement>(new HttpRequestMessage(HttpMethod.Get, $"api/concepts/{conceptId:D}/revision"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<JsonElement>> ListDeletedConceptsAsync(Guid profileId, int page, CancellationToken cancellationToken) =>
        SendAsync<JsonElement>(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/concepts/deleted?profileId={profileId:D}&page={Math.Max(1, page).ToString(System.Globalization.CultureInfo.InvariantCulture)}"),
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<JsonElement>> GetDeletedConceptAsync(Guid conceptId, CancellationToken cancellationToken) =>
        SendAsync<JsonElement>(new HttpRequestMessage(HttpMethod.Get, $"api/concepts/deleted/{conceptId:D}"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<JournalDayResponse>> GetJournalDayAsync(string? date, string? filter, int skip, CancellationToken cancellationToken) =>
        SendAsync<JournalDayResponse>(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/journal?date={Uri.EscapeDataString(date ?? string.Empty)}&filter={Uri.EscapeDataString(filter ?? string.Empty)}&skip={Math.Max(0, skip).ToString(System.Globalization.CultureInfo.InvariantCulture)}"),
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<ErrorReportPreviewResponse>> GetErrorReportPreviewAsync(CancellationToken cancellationToken) =>
        SendAsync<ErrorReportPreviewResponse>(new HttpRequestMessage(HttpMethod.Get, "api/error-reports/preview"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ErrorReportReceiptResponse>> SubmitErrorReportAsync(ErrorReportApiRequest? request, CancellationToken cancellationToken) =>
        SendAsync<ErrorReportReceiptResponse>(
            new HttpRequestMessage(HttpMethod.Post, "api/error-reports") { Content = JsonContent.Create(request, options: Json) },
            cancellationToken);

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
        HttpStatusCode.Forbidden => ErrorType.Forbidden,
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
            // Gövdesiz 401/403/429 (ör. yetki politikasının 403'ü) türünü korur: "API erişilemiyor" sayılmaz.
            return type switch
            {
                ErrorType.Forbidden => ApiClientErrors.Forbidden,
                ErrorType.Unauthorized or ErrorType.TooManyRequests => new Error("api.error", ApiClientErrors.Unavailable.Message, type),
                _ => ApiClientErrors.Unavailable,
            };
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
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                // Başarılı durum koduyla bozuk/JSON olmayan gövde (review R3-3): istisna sayfayı çökertmez, "API erişilemiyor" olur.
                return Result<T>.Failure(ApiClientErrors.Unavailable);
            }
        }
    }
}
