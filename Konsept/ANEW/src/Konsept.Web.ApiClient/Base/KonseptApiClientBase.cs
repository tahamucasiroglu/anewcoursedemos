using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;

namespace Konsept.Web.ApiClient.Base;

/// <summary>
/// API çağrılarının ortak HTTP mantığı: başarıda JSON gövdesini okur; ProblemDetails'i
/// <see cref="Error"/>'a çevirir; ağ hatası ve zaman aşımı <see cref="ApiClientErrors.Unavailable"/> olur.
/// </summary>
public abstract class KonseptApiClientBase : IKonseptApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public Task<Result<SystemStatusResponse>> GetSystemStatusAsync(CancellationToken cancellationToken) =>
        SendAsync<SystemStatusResponse>(new HttpRequestMessage(HttpMethod.Get, "api/system/status"), cancellationToken);

    /// <inheritdoc />
    public Task<Result<AgentTestResponse>> TestAgentAsync(CancellationToken cancellationToken) =>
        SendAsync<AgentTestResponse>(new HttpRequestMessage(HttpMethod.Post, "api/agent/test"), cancellationToken);

    /// <summary>İsteği API'ye gönderir.</summary>
    /// <param name="request">HTTP isteği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>HTTP cevabı.</returns>
    protected abstract Task<HttpResponseMessage> TransmitAsync(HttpRequestMessage request, CancellationToken cancellationToken);

    private static ErrorType TypeOf(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => ErrorType.Validation,
        HttpStatusCode.NotFound => ErrorType.NotFound,
        HttpStatusCode.Conflict => ErrorType.Conflict,
        HttpStatusCode.GatewayTimeout => ErrorType.Timeout,
        _ => ErrorType.Unavailable,
    };

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

    private static async Task<Error> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json, cancellationToken).ConfigureAwait(false);
            var code = problem.TryGetProperty("code", out var c) ? c.GetString() : null;
            var title = problem.TryGetProperty("title", out var t) ? t.GetString() : null;
            return new Error(code ?? "api.error", title ?? ApiClientErrors.Unavailable.Message, TypeOf(response.StatusCode));
        }
        catch (JsonException)
        {
            return ApiClientErrors.Unavailable;
        }
    }
}
