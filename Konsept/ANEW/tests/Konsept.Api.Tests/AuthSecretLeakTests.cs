using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Kimlik bilgisi sızıntısı yok (spec 0002 AC-17, Api tarafı): kayıt, giriş (başarılı/başarısız), refresh ve çıkış
/// sırasında üretilen dosya logu, teknik log, olay kaydı ve hata cevapları şifreyi ya da token'ları içermez; yapılandırılmış
/// imza anahtarı bilinen sır olarak hiçbir logda görünmez.
/// </summary>
public sealed class AuthSecretLeakTests : IClassFixture<FastApiFactory>
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);
    private readonly FastApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AuthSecretLeakTests"/> class.</summary>
    /// <param name="factory">Hızlı API fabrikası (gerçek saat; teknik log sink'i çalışır).</param>
    public AuthSecretLeakTests(FastApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Tüm kimlik akışlarından sonra hiçbir log, olay ya da hata cevabı şifre veya token içermez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AuthFlows_NoPasswordOrTokenInLogsEventsOrProblems()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var password = "Gizli" + Guid.NewGuid().ToString("N")[..10] + "7";
        var wrongPassword = "Yanlis" + Guid.NewGuid().ToString("N")[..10] + "3";
        var email = AuthenticatedClient.NewEmail();
        var bodies = new List<string>();
        var secrets = new List<string> { password, wrongPassword };

        var registered = await AuthenticatedClient.RegisterAsync(client, _factory.Services, email, password, "Sızıntı", ct);
        bodies.Add(await PostTextAsync(client, "/api/auth/login", new { email, password = wrongPassword }, ct));
        bodies.Add(await PostTextAsync(client, "/api/auth/register", new { name = "x", email, password }, ct));
        bodies.Add(await PostTextAsync(client, "/api/auth/register", new { name = "x", email = AuthenticatedClient.NewEmail(), password = "kisa" }, ct));
        var session = await AuthenticatedClient.LoginAsync(client, _factory.Services, email, password, ct);
        using var refresh = await AuthenticatedClient.PostAsync(client, _factory.Services, "/api/auth/refresh", new { refreshToken = session.GetProperty("refreshToken").GetString() }, ct);
        var refreshed = await refresh.Content.ReadFromJsonAsync<JsonElement>(ct);
        bodies.Add(await PostTextAsync(client, "/api/auth/refresh", new { refreshToken = session.GetProperty("refreshToken").GetString() }, ct));
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.GetProperty("accessToken").GetString());
        (await client.SendAsync(logout, ct)).EnsureSuccessStatusCode();
        foreach (var issued in new[] { registered, session, refreshed })
        {
            secrets.Add(issued.GetProperty("accessToken").GetString()!);
            secrets.Add(issued.GetProperty("refreshToken").GetString()!);
        }

        var text = await FlushedLogTextAsync(ct);

        foreach (var secret in secrets)
        {
            text.ShouldNotContain(secret);
            bodies.ShouldAllBe(b => !b.Contains(secret));
            _factory.Events.Events.ShouldAllBe(e => !e.Message.Contains(secret));
            _factory.TechnicalLogs.Logs.ShouldAllBe(l => !l.Message.Contains(secret) && !(l.Exception ?? string.Empty).Contains(secret));
        }

        text.ShouldContain("Refresh reddedildi");
        _factory.Users.Get(email).PasswordHash.ShouldNotContain(password);
    }

    /// <summary>Yapılandırmadaki <c>Auth:SigningKey</c> bilinen sırdır: etiketsiz loglansa da dosyada ve teknik logda görünmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SigningKey_InConfig_NeverLogged()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");
        var logger = _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Api.Tests.ConfigLeak");

#pragma warning disable CA1848, CA2254 // Test: anahtar kasıtlı olarak serbest metinde.
        logger.LogWarning($"imza {marker} denendi {FastApiFactory.SigningKey} bitti");
#pragma warning restore CA1848, CA2254

        await Eventually.WithinAsync(() => Task.FromResult(_factory.ReadLogText().Contains(marker, StringComparison.Ordinal)), Limit, ct);
        await Eventually.WithinAsync(() => Task.FromResult(_factory.TechnicalLogs.Logs.Any(l => l.Message.Contains(marker, StringComparison.Ordinal))), Limit, ct);
        _factory.ReadLogText().ShouldNotContain(FastApiFactory.SigningKey);
        _factory.TechnicalLogs.Logs.Single(l => l.Message.Contains(marker, StringComparison.Ordinal)).Message.ShouldNotContain(FastApiFactory.SigningKey);
    }

    private async Task<string> PostTextAsync(HttpClient client, string path, object body, CancellationToken ct)
    {
        using var response = await AuthenticatedClient.PostAsync(client, _factory.Services, path, body, ct);
        response.IsSuccessStatusCode.ShouldBeFalse();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private async Task<string> FlushedLogTextAsync(CancellationToken ct)
    {
        var marker = Guid.NewGuid().ToString("N");
        var logger = _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Api.Tests.Flush");
#pragma warning disable CA1848, CA2254 // Test: dosya logunun yazıldığını gösteren işaret.
        logger.LogWarning($"flush {marker}");
#pragma warning restore CA1848, CA2254
        await Eventually.WithinAsync(() => Task.FromResult(_factory.ReadLogText().Contains(marker, StringComparison.Ordinal)), Limit, ct);
        return _factory.ReadLogText();
    }
}
