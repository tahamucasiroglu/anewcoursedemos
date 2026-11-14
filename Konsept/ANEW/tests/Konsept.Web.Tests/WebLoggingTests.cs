using Konsept.Shared.Kernel.Results;
using Konsept.Shared.Kernel.Security;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Concrete;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Infrastructure;
using Konsept.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Serilog.Events;
using Serilog.Parsing;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Web teknik logu (spec 0008 AC-1, AC-6 Web kısmı, AC-8; review round 1, M-2): gerçek Web host'unda dosya maskeli
/// ve kaynak "Web"; Warning+ kayıtlar maskeli olarak API istemcisine iletilir, Information iletilmez; döngü kalkanı;
/// istemci servis anahtarı başlığını ekler.
/// </summary>
public sealed class WebLoggingTests : IClassFixture<WebFactory>
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);
    private readonly WebFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="WebLoggingTests"/> class.</summary>
    /// <param name="factory">Web fabrikası.</param>
    public WebLoggingTests(WebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Web host'unun logger'ı: dosyada kaynak "Web" ve gizli alanlar maskeli; Warning API'ye maskeli iletilir,
    /// Information iletilmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task WebLog_HostLogger_FileMaskedAndWarningForwarded()
    {
        var ct = TestContext.Current.CancellationToken;
        var forwarded = new List<ForwardedLogRequest>();
        _factory.Api.ForwardLogsAsync(Arg.Do<IReadOnlyList<ForwardedLogRequest>>(l => { lock (forwarded) { forwarded.AddRange(l); } }), Arg.Any<CancellationToken>())
            .Returns(call => Result<int>.Success(call.Arg<IReadOnlyList<ForwardedLogRequest>>().Count));
        using var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");
        var logger = _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Web.Tests.Marker");

#pragma warning disable CA1848, CA2254 // Test: farklı seviyelerde gizli alanlarla tek tek yazılıyor.
        logger.LogInformation("bilgi {Marker}", marker);
        logger.LogWarning("uyarı {Marker} {Password} {ApiKey} {PairingCode}", marker, "web-pw-1", "sk-proj-WEBKEY123456-w9w", "PAIR-5566");
#pragma warning restore CA1848, CA2254

        await Eventually(() => { lock (forwarded) { return forwarded.Any(f => f.Message.Contains(marker, StringComparison.Ordinal)); } }, ct);
        await Eventually(() => _factory.ReadLogText().Contains($"uyarı {marker}", StringComparison.Ordinal), ct);

        List<ForwardedLogRequest> mine;
        lock (forwarded)
        {
            mine = [.. forwarded.Where(f => f.Message.Contains(marker, StringComparison.Ordinal))];
        }

        mine.Count.ShouldBe(1);
        mine[0].Level.ShouldBe(3);
        mine[0].Message.ShouldNotContain("web-pw-1");
        mine[0].Message.ShouldNotContain("WEBKEY123456");
        mine[0].Message.ShouldNotContain("PAIR-5566");
        mine[0].Message.ShouldContain("sk-…w9w");
        var text = _factory.ReadLogText();
        text.ShouldContain($" Web ");
        foreach (var secret in new[] { "web-pw-1", "WEBKEY123456", "PAIR-5566" })
        {
            text.ShouldNotContain(secret);
        }
    }

    /// <summary>
    /// Web'in kendi servis anahtarı açılışta bilinen sır olarak kaydedilir: mesajda etiketsiz geçse de ne dosyada ne
    /// API'ye iletilen kayıtta açık görünür (review r4, L-3).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task WebLog_ConfiguredServiceKeyInMessage_NeverWritten()
    {
        var ct = TestContext.Current.CancellationToken;
        var forwarded = new List<ForwardedLogRequest>();
        _factory.Api.ForwardLogsAsync(Arg.Do<IReadOnlyList<ForwardedLogRequest>>(l => { lock (forwarded) { forwarded.AddRange(l); } }), Arg.Any<CancellationToken>())
            .Returns(call => Result<int>.Success(call.Arg<IReadOnlyList<ForwardedLogRequest>>().Count));
        using var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");
        var logger = _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Web.Tests.KnownSecretMarker");

#pragma warning disable CA1848, CA2254 // Test: anahtar düz metin olarak mesajda.
        logger.LogWarning($"uyarı {marker} retry with {WebFactory.ServiceKey} failed");
#pragma warning restore CA1848, CA2254

        await Eventually(() => { lock (forwarded) { return forwarded.Any(f => f.Message.Contains(marker, StringComparison.Ordinal)); } }, ct);
        await Eventually(() => _factory.ReadLogText().Contains($"uyarı {marker}", StringComparison.Ordinal), ct);

        lock (forwarded)
        {
            forwarded.Where(f => f.Message.Contains(marker, StringComparison.Ordinal)).ShouldAllBe(f => !f.Message.Contains(WebFactory.ServiceKey));
        }

        _factory.ReadLogText().ShouldNotContain(WebFactory.ServiceKey);
    }

    /// <summary>Yalnızca Warning+ ve dışlanmamış kategoriler iletilir (HTTP ve API istemcisinin kendi logları döngü yapmasın).</summary>
    /// <param name="level">Seviye.</param>
    /// <param name="category">Kategori.</param>
    /// <param name="expected">İletilir mi.</param>
    [Theory]
    [InlineData(LogEventLevel.Information, "Konsept.Web", false)]
    [InlineData(LogEventLevel.Warning, "Konsept.Web", true)]
    [InlineData(LogEventLevel.Fatal, "Konsept.Web", true)]
    [InlineData(LogEventLevel.Error, "System.Net.Http.HttpClient.IKonseptApiClient.LogicalHandler", false)]
    [InlineData(LogEventLevel.Error, "Microsoft.Extensions.Http.DefaultHttpClientFactory", false)]
    [InlineData(LogEventLevel.Error, "Konsept.Web.ApiClient.Concrete.KonseptApiClient", false)]
    [InlineData(LogEventLevel.Warning, "Konsept.Web.Infrastructure.ApiLogForwardingSink", false)]
    public void ApiLogForwarding_Filter_WarningPlusAndNotExcluded(LogEventLevel level, string category, bool expected)
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            null,
            new MessageTemplateParser().Parse("m"),
            [new LogEventProperty("SourceContext", new ScalarValue(category))]);

        ApiLogForwardingSink.ShouldForward(logEvent).ShouldBe(expected);
    }

    /// <summary>İletim isteği <c>api/internal/logs</c>'a servis anahtarı başlığıyla gider; anahtar yoksa başlık eklenmez.</summary>
    /// <param name="key">Servis anahtarı.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("test-key-1")]
    [InlineData(null)]
    public async Task ApiClient_ForwardLogs_SendsServiceKeyHeader(string? key)
    {
        var handler = new RecordingHandler("1");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
        var client = new KonseptApiClient(http, new ApiClientOptions(key));

        var result = await client.ForwardLogsAsync([new ForwardedLogRequest(DateTimeOffset.UtcNow, 3, 0, "uyarı", null, null, null)], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var request = handler.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri!.AbsolutePath.ShouldBe("/api/internal/logs");
        if (key is null)
        {
            request.Headers.Contains(ServiceKeyHeader.Name).ShouldBeFalse();
        }
        else
        {
            request.Headers.GetValues(ServiceKeyHeader.Name).Single().ShouldBe(key);
        }
    }

    private static async Task Eventually(Func<bool> condition, CancellationToken ct)
    {
        var until = DateTime.UtcNow + Limit;
        while (!condition())
        {
            if (DateTime.UtcNow > until)
            {
                throw new TimeoutException($"Koşul {Limit.TotalSeconds} sn içinde sağlanmadı.");
            }

            await Task.Delay(100, ct);
        }
    }
}
