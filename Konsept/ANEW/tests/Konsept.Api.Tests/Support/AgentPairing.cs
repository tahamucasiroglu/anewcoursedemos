using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Ajan eşleştirme uçlarını gerçek HTTP yoluyla çağırır (spec 0012): ajanın kod alması ve durum sorgusu (kimliksiz), web'in
/// sorgu/onay/ret çağrıları (kullanıcının Bearer istemcisiyle). Servis anahtarı yapılandırılmışsa her istek benzersiz bir
/// istemci IP'si iletir; testler birbirinin hız sınırı penceresini tüketmez (<see cref="AuthenticatedClient"/> ile aynı ilke).
/// </summary>
public static class AgentPairing
{
    /// <summary>Kod alır (başarı beklenir).</summary>
    /// <param name="client">İstemci (kimliksiz olabilir).</param>
    /// <param name="services">Api servisleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <param name="machineName">Makine adı.</param>
    /// <param name="agentVersion">Ajan sürümü.</param>
    /// <returns>Kod ve sır.</returns>
    public static async Task<(string Code, string Secret, DateTimeOffset ExpiresAt)> CreateCodeAsync(
        HttpClient client, IServiceProvider services, CancellationToken cancellationToken, string machineName = "TAHA-PC", string agentVersion = "0.5.0")
    {
        using var response = await SendAsync(client, services, HttpMethod.Post, "/api/agent-pairing/codes", new { machineName, agentVersion }, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Kod alınamadı: {(int)response.StatusCode}");
        }

        return (body.GetProperty("code").GetString()!, body.GetProperty("secret").GetString()!, body.GetProperty("expiresAt").GetDateTimeOffset());
    }

    /// <summary>Kod durumunu sorar.</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="services">Api servisleri.</param>
    /// <param name="secret">Onay sırrı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum ve (varsa) ajan belirteci.</returns>
    public static async Task<(string State, string? Token)> StatusAsync(HttpClient client, IServiceProvider services, string secret, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(client, services, HttpMethod.Post, "/api/agent-pairing/status", new { secret }, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken).ConfigureAwait(false);
        var token = body.TryGetProperty("agentToken", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        return (body.GetProperty("state").GetString()!, token);
    }

    /// <summary>Web tarafı çağrısı: sorgu (GET), onay ya da ret (POST).</summary>
    /// <param name="client">Kullanıcının Bearer istemcisi.</param>
    /// <param name="services">Api servisleri.</param>
    /// <param name="action"><c>lookup</c>, <c>approve</c> ya da <c>reject</c>.</param>
    /// <param name="code">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <param name="ip">İletilecek istemci IP'si (verilmezse benzersiz).</param>
    /// <returns>Cevap.</returns>
    public static Task<HttpResponseMessage> DecideAsync(HttpClient client, IServiceProvider services, string action, string code, CancellationToken cancellationToken, string? ip = null) =>
        action == "lookup"
            ? SendAsync(client, services, HttpMethod.Get, "/api/agent-pairing/" + Uri.EscapeDataString(code), null, cancellationToken, ip)
            : SendAsync(client, services, HttpMethod.Post, $"/api/agent-pairing/{Uri.EscapeDataString(code)}/{action}", null, cancellationToken, ip);

    /// <summary>Tam eşleştirme: kod al → kullanıcı onaylar → ajan belirtecini alır.</summary>
    /// <param name="userClient">Kodu onaylayacak kullanıcının istemcisi.</param>
    /// <param name="agentClient">Ajanın (kimliksiz) istemcisi.</param>
    /// <param name="services">Api servisleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <param name="machineName">Makine adı.</param>
    /// <returns>Ajan oturum belirteci.</returns>
    public static async Task<string> PairAsync(HttpClient userClient, HttpClient agentClient, IServiceProvider services, CancellationToken cancellationToken, string machineName = "TAHA-PC")
    {
        var (code, secret, _) = await CreateCodeAsync(agentClient, services, cancellationToken, machineName).ConfigureAwait(false);
        using var approve = await DecideAsync(userClient, services, "approve", code, cancellationToken).ConfigureAwait(false);
        if (approve.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Onay başarısız: {(int)approve.StatusCode}");
        }

        var (state, token) = await StatusAsync(agentClient, services, secret, cancellationToken).ConfigureAwait(false);
        return state == "approved" && token is not null ? token : throw new InvalidOperationException($"Belirteç alınamadı: {state}");
    }

    /// <summary>İstek gönderir: servis anahtarı yapılandırılmışsa anahtar ve istemci IP'si başlıklarıyla.</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="services">Api servisleri.</param>
    /// <param name="method">Yöntem.</param>
    /// <param name="path">Yol.</param>
    /// <param name="body">Gövde (yoksa <c>null</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <param name="ip">İstemci IP'si (verilmezse benzersiz).</param>
    /// <returns>Cevap.</returns>
    public static Task<HttpResponseMessage> SendAsync(HttpClient client, IServiceProvider services, HttpMethod method, string path, object? body, CancellationToken cancellationToken, string? ip = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(services);
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var key = services.GetRequiredService<IConfiguration>()[ServiceKeyHeader.ConfigurationKey];
        if (!string.IsNullOrEmpty(key))
        {
            request.Headers.Add(ServiceKeyHeader.Name, key);
            request.Headers.Add(ClientIpHeader.Name, ip ?? AuthenticatedClient.NewIp());
        }

        return client.SendAsync(request, cancellationToken);
    }
}
