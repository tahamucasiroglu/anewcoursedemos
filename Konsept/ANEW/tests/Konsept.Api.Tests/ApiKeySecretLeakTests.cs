using System.Net.Http.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Key sızıntısı yok (spec 0004 AC-5, H-2), 0008'in tarama yöntemiyle (<see cref="AuthSecretLeakTests"/> ile aynı): kaydetme,
/// doğrulama (başarılı ve 401), değiştirme, biçim hatası ve silme akışlarında üretilen dosya logu, veritabanı teknik logu, Günlük
/// olayları ve tüm yanıt gövdeleri taranır; girilen key'in düz hali (ve 8+ karakterlik parçası) hiçbirinde yoktur, log yalnızca
/// maskeli hali taşır. Kaydedilen key bilinen sırdır: bir log metnine elle yazılsa da gizlenir.
/// </summary>
public sealed class ApiKeySecretLeakTests : IClassFixture<AiGatewayApiFactory>
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);
    private readonly AiGatewayApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ApiKeySecretLeakTests"/> class.</summary>
    /// <param name="factory">AiGateway test fabrikası (gerçek saat; teknik log sink'i çalışır).</param>
    public ApiKeySecretLeakTests(AiGatewayApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Tüm key akışlarından sonra hiçbir log, olay ya da yanıt düz key içermez; log maskeli hali içerir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task KeyFlows_NoPlainKeyInLogsEventsOrResponses()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var saved = ApiKeyApiTests.NewKey("sk-ant-");
        var replaced = ApiKeyApiTests.NewKey("sk-ant-");
        var rejected = ApiKeyApiTests.NewKey("sk-ant-");
        var unprefixed = ApiKeyApiTests.NewKey("glmkey"); // Bilinen önek yok: 0008 desenleri değil, bilinen sır kaydı gizlemeli.
        var bodies = new List<string>();

        _factory.Validator.Outcome = KeyCheckOutcome.Accepted;
        bodies.Add(await PutTextAsync(client, saved, ct));
        bodies.Add(await PutTextAsync(client, replaced, ct));
        _factory.Validator.Outcome = KeyCheckOutcome.Rejected;
        bodies.Add(await PutTextAsync(client, rejected, ct));
        _factory.Validator.Outcome = KeyCheckOutcome.Accepted;
        bodies.Add(await PutTextAsync(client, "kisa\u0001" + saved[..10], ct));
        using (var glm = await client.PutAsJsonAsync("/api/ai/keys/glm", new { apiKey = unprefixed }, ct))
        {
            bodies.Add(await glm.Content.ReadAsStringAsync(ct));
        }
        bodies.Add(await client.GetStringAsync(new Uri("/api/ai/keys", UriKind.Relative), ct));
        bodies.Add(await client.GetStringAsync(new Uri("/api/ai/catalog", UriKind.Relative), ct));
        using (var delete = await client.DeleteAsync(new Uri("/api/ai/keys/anthropic", UriKind.Relative), ct))
        {
            bodies.Add(await delete.Content.ReadAsStringAsync(ct));
        }

        // Bilinen sır: key bir log metnine elle girse de (ör. ileride bir hata mesajı) dosyada ve teknik logda görünmez.
        var logger = _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Api.Tests.KeyLeak");
#pragma warning disable CA1848, CA2254 // Test: key kasıtlı olarak serbest metinde.
        logger.LogWarning($"elle yazılan key {replaced} sonu");
        logger.LogWarning($"elle yazılan öneksiz key {unprefixed} sonu");
#pragma warning restore CA1848, CA2254
        var text = await FlushedLogTextAsync(ct);

        foreach (var key in new[] { saved, replaced, rejected, unprefixed })
        {
            ApiKeyApiTests.ShouldNotLeak(key, [text, .. bodies]);
            ApiKeyApiTests.ShouldNotLeak(key, [.. _factory.Events.Events.Select(e => e.Message)]);
            ApiKeyApiTests.ShouldNotLeak(key, [.. _factory.TechnicalLogs.Logs.Select(l => l.Message + (l.Exception ?? string.Empty))]);
        }

        text.ShouldContain("API key kaydedildi");
        text.ShouldContain("sk-…" + saved[^3..]);
        text.ShouldContain("reddetti");
        text.ShouldContain("elle yazılan key");
        text.ShouldContain("elle yazılan öneksiz key");
    }

    private async Task<string> PutTextAsync(HttpClient client, string key, CancellationToken ct)
    {
        using var response = await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = key }, ct);
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
        await Eventually.WithinAsync(() => Task.FromResult(_factory.TechnicalLogs.Logs.Any(l => l.Message.Contains(marker, StringComparison.Ordinal))), Limit, ct);
        return _factory.ReadLogText();
    }
}
