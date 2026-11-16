using System.Net;
using System.Net.Http.Json;
using System.Text;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Çağrı hatalarında sızıntı yok (spec 0013 AC-8, Req 9), 0008'in tarama yöntemiyle (<see cref="ApiKeySecretLeakTests"/> ile aynı):
/// gerçek sağlayıcı istemcisi sahte HTTP işleyicisiyle 400/401/402/403/404 döndürür ve hata gövdesine sağlayıcının ham metnini ve
/// gönderilen key'i koyar. Üretilen hata yanıtları, dosya logu, veritabanı teknik logu ve Günlük olayları taranır: ham yanıt ya da
/// key'in düz hali hiçbirinde yoktur; her durumda tam 1 istek gider ve kullanıcıya Türkçe mesaj döner.
/// </summary>
public sealed class AiCallSecretLeakTests : IAsyncLifetime
{
    private const string RawMarker = "HAM-SAGLAYICI-YANITI";
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);
    private readonly RecordingProviderHandler _handler;
    private readonly AiGatewayApiFactory _factory;
    private int _status = 401;

    /// <summary>Initializes a new instance of the <see cref="AiCallSecretLeakTests"/> class.</summary>
    public AiCallSecretLeakTests()
    {
        _handler = new RecordingProviderHandler(request =>
        {
            var key = request.Headers.TryGetValues("x-api-key", out var values) ? values.Single() : request.Headers.Authorization?.Parameter;
            return new HttpResponseMessage((HttpStatusCode)_status)
            {
                Content = new StringContent("{\"error\":{\"message\":\"" + RawMarker + " invalid key " + key + "\"}}", Encoding.UTF8, "application/json"),
            };
        });
        _factory = new AiGatewayApiFactory { ProviderHandler = _handler };
    }

    /// <summary>AC-8: 4xx yanıtlarında tek deneme, Türkçe mesaj; hiçbir yanıt, log ya da Günlük satırında ham yanıt ya da key yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TrialFlows_NoRawProviderBodyOrKeyInLogsEventsOrResponses()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var key = ApiKeyApiTests.NewKey("glmkey"); // Öneksiz: 0008 desenleri değil, bilinen sır kaydı ve kapının kendisi gizlemeli.
        (await client.PutAsJsonAsync("/api/ai/keys/glm", new { apiKey = key }, ct)).EnsureSuccessStatusCode();
        var bodies = new List<string>();
        var expected = new Dictionary<int, string>
        {
            [400] = "Sağlayıcı isteği kabul etmedi",
            [401] = "Sağlayıcı API key'i reddetti",
            [402] = "Sağlayıcı hesabında yeterli bakiye yok",
            [403] = "Sağlayıcı bu isteğe izin vermedi",
            [404] = "Sağlayıcı isteği kabul etmedi",
        };

        foreach (var (status, message) in expected)
        {
            _status = status;
            var before = _handler.Requests.Count;
            using var response = await client.PostAsJsonAsync("/api/ai/calls/trial", new { modelId = "glm/glm-4.6" }, ct);
            var text = await response.Content.ReadAsStringAsync(ct);
            bodies.Add(text);
            text.ShouldContain(message);
            (_handler.Requests.Count - before).ShouldBe(1, $"{status}");
        }

        var log = await FlushedLogTextAsync(ct);
        string[] all = [log, .. bodies, .. _factory.Events.Events.Select(e => e.Message), .. _factory.TechnicalLogs.Logs.Select(l => l.Message + (l.Exception ?? string.Empty))];
        ApiKeyApiTests.ShouldNotLeak(key, all);
        foreach (var text in all)
        {
            text.ShouldNotContain(RawMarker);
        }

        log.ShouldContain("Model çağrısı denemesi başarısız");
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _handler.Dispose();
    }

    private async Task<string> FlushedLogTextAsync(CancellationToken ct)
    {
        var marker = Guid.NewGuid().ToString("N");
        var logger = _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Api.Tests.Flush");
#pragma warning disable CA1848, CA2254 // Test: dosya logunun yazıldığını gösteren işaret.
        logger.LogWarning($"flush {marker}");
#pragma warning restore CA1848, CA2254
        await Eventually.WithinAsync(() => Task.FromResult(_factory.ReadLogText().Contains(marker, StringComparison.Ordinal)), Limit, ct);
        await Eventually.WithinAsync(() => Task.FromResult(_factory.TechnicalLogs.Logs.Any(l => l.Message.Contains(marker, StringComparison.Ordinal))), Limit, ct);
        return _factory.ReadLogText();
    }
}
