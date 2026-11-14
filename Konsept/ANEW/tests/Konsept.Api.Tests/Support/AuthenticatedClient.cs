using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Security;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Default-deny Api'de kimlikli istemci (plan 0002, Risk 8): gerçek <c>api/auth/register</c> + <c>api/auth/login</c>
/// uçlarıyla giriş yapar ve Bearer token'ı istemciye ekler. Her istek, servis anahtarıyla birlikte benzersiz bir istemci
/// IP'si iletir; böylece testler birbirinin hız sınırı penceresini tüketmez.
/// </summary>
public static class AuthenticatedClient
{
    /// <summary>Testlerde kullanılan geçerli şifre.</summary>
    public const string Password = "Sifre1234";

    private static int _ipCounter;

    /// <summary>Benzersiz test e-postası.</summary>
    /// <returns>E-posta.</returns>
    public static string NewEmail() => $"u{Guid.NewGuid():N}@test.local";

    /// <summary>Benzersiz test istemci IP'si (10.x.y.z).</summary>
    /// <returns>IP.</returns>
    public static string NewIp()
    {
        var n = Interlocked.Increment(ref _ipCounter);
        return $"10.{(n >> 16) & 255}.{(n >> 8) & 255}.{n & 255}";
    }

    /// <summary>Kayıt olur, giriş yapar ve Bearer token'lı istemci döndürür.</summary>
    /// <param name="factory">Api fabrikası.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <param name="email">E-posta (verilmezse benzersiz).</param>
    /// <param name="name">Ad.</param>
    /// <returns>İstemci.</returns>
    public static async Task<HttpClient> CreateAsync(
        WebApplicationFactory<Program> factory,
        CancellationToken cancellationToken,
        string? email = null,
        string name = "Test Kullanıcı")
    {
        ArgumentNullException.ThrowIfNull(factory);
        var client = factory.CreateClient();
        var address = email ?? NewEmail();
        await RegisterAsync(client, factory.Services, address, Password, name, cancellationToken).ConfigureAwait(false);
        var session = await LoginAsync(client, factory.Services, address, Password, cancellationToken).ConfigureAwait(false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        return client;
    }

    /// <summary>Kayıt olur (başarı beklenir).</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="services">Api servisleri (servis anahtarı ayarı için).</param>
    /// <param name="email">E-posta.</param>
    /// <param name="password">Şifre.</param>
    /// <param name="name">Ad.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oturum gövdesi.</returns>
    public static async Task<JsonElement> RegisterAsync(HttpClient client, IServiceProvider services, string email, string password, string name, CancellationToken cancellationToken)
    {
        using var response = await PostAsync(client, services, "/api/auth/register", new { name, email, password }, cancellationToken).ConfigureAwait(false);
        return await ReadSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Giriş yapar (başarı beklenir).</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="services">Api servisleri.</param>
    /// <param name="email">E-posta.</param>
    /// <param name="password">Şifre.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oturum gövdesi.</returns>
    public static async Task<JsonElement> LoginAsync(HttpClient client, IServiceProvider services, string email, string password, CancellationToken cancellationToken)
    {
        using var response = await PostAsync(client, services, "/api/auth/login", new { email, password }, cancellationToken).ConfigureAwait(false);
        return await ReadSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>JSON gövdeli POST: servis anahtarı yapılandırılmışsa anahtar ve istemci IP'si başlıklarıyla.</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="services">Api servisleri.</param>
    /// <param name="path">Yol.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <param name="ip">İletilecek istemci IP'si (verilmezse benzersiz).</param>
    /// <returns>Cevap.</returns>
    public static Task<HttpResponseMessage> PostAsync(HttpClient client, IServiceProvider services, string path, object body, CancellationToken cancellationToken, string? ip = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(services);
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        var key = services.GetRequiredService<IConfiguration>()[ServiceKeyHeader.ConfigurationKey];
        if (!string.IsNullOrEmpty(key))
        {
            request.Headers.Add(ServiceKeyHeader.Name, key);
            request.Headers.Add(ClientIpHeader.Name, ip ?? NewIp());
        }

        return client.SendAsync(request, cancellationToken);
    }

    private static async Task<JsonElement> ReadSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken).ConfigureAwait(false);
        return response.StatusCode == HttpStatusCode.OK
            ? body
            : throw new InvalidOperationException($"Kimlik ucu başarısız: {(int)response.StatusCode} {body}");
    }
}
