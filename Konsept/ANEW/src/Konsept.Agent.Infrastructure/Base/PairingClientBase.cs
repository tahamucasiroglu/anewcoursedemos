using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Eşleştirme uçlarının ajan tarafı (spec 0012): <c>POST api/agent-pairing/codes</c> ve <c>POST api/agent-pairing/status</c>.
/// Başarısız durum kodu (429 dahil) ve ağ hatası <see cref="HttpRequestException"/> olur; akış yeniden dener. Gövdeler loglanmaz.
/// Gönderim alt sınıftadır.
/// </summary>
public abstract class PairingClientBase : IPairingClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<PairingCodeInfo> RequestCodeAsync(string machineName, string agentVersion, CancellationToken cancellationToken)
    {
        var body = await PostAsync("api/agent-pairing/codes", new { machineName, agentVersion }, cancellationToken).ConfigureAwait(false);
        return new PairingCodeInfo(
            body.GetProperty("code").GetString() ?? throw new HttpRequestException("Kod alınamadı."),
            body.GetProperty("secret").GetString() ?? throw new HttpRequestException("Kod alınamadı."),
            body.GetProperty("expiresAt").GetDateTimeOffset());
    }

    /// <inheritdoc />
    public async Task<PairingPoll> CheckAsync(string secret, CancellationToken cancellationToken)
    {
        var body = await PostAsync("api/agent-pairing/status", new { secret }, cancellationToken).ConfigureAwait(false);
        var token = body.TryGetProperty("agentToken", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        var state = body.GetProperty("state").GetString() switch
        {
            "pending" => PairingPollState.Pending,
            "approved" => PairingPollState.Approved,
            "rejected" => PairingPollState.Rejected,
            _ => PairingPollState.Expired,
        };
        return new PairingPoll(state, token);
    }

    /// <summary>İsteği gönderir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap.</returns>
    protected abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

    private async Task<JsonElement> PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: Json) };
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Eşleştirme isteği başarısız: {(int)response.StatusCode}", null, response.StatusCode);
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>(Json, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException("Eşleştirme cevabı okunamadı.", ex);
        }
    }
}
