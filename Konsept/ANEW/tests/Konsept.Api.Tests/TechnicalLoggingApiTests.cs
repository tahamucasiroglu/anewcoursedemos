using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Monitoring.Infrastructure.Logging;
using Konsept.Shared.Kernel.Security;
using Konsept.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Events;
using Serilog.Parsing;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Api teknik logu (spec 0008): korelasyon (AC-2), veritabanı sink'i süzgeci (AC-6), Web log iletimi ve servis
/// anahtarı (AC-6, plan Risk 2), ajan özeti kabul kuralları (AC-7), maskeleme (AC-8, Api kısmı).
/// </summary>
public sealed class TechnicalLoggingApiTests : IClassFixture<FastApiFactory>
{
    /// <summary>Gets AC-8 tam alan tablosundaki alan adları.</summary>
    public static TheoryData<string> SecretFields => [.. SecretFieldSamples.All.Select(s => s.Field)];

    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);
    private readonly FastApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="TechnicalLoggingApiTests"/> class.</summary>
    /// <param name="factory">Hızlı API fabrikası.</param>
    public TechnicalLoggingApiTests(FastApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// ProblemDetails hata yanıtındaki korelasyon numarasıyla Api log dosyasında ve veritabanı teknik logunda ilgili
    /// hata kaydı bulunur; hata mesajındaki key logda maskelidir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProblemDetails_CorrelationId_FoundInApiLog()
    {
        var ct = TestContext.Current.CancellationToken;
        var logDirectory = Path.Combine(_factory.LogDirectory, "failing-" + Guid.NewGuid().ToString("N"));
        var failing = _factory.WithWebHostBuilder(b => b
            .UseSetting("Logging:Directory", logDirectory)
            .ConfigureTestServices(s =>
            {
                s.RemoveAll<ISystemStatusService>();
                s.AddScoped<ISystemStatusService, ThrowingSystemStatusService>();
            }));
        using var client = await AuthenticatedClient.CreateAsync(failing, ct);

        using var response = await client.GetAsync(new Uri("/api/system/status", UriKind.Relative), ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var correlationId = problem.GetProperty("correlationId").GetString()!;

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        correlationId.ShouldNotBeNullOrWhiteSpace();
        await Eventually.WithinAsync(() => Task.FromResult(FastApiFactory.ReadLogText(logDirectory).Contains(correlationId, StringComparison.Ordinal)), Limit, ct);
        var line = FastApiFactory.ReadLogText(logDirectory).Split('\n').First(l => l.Contains(correlationId, StringComparison.Ordinal) && l.Contains("[ERR]", StringComparison.Ordinal));
        line.ShouldContain(" Api ");
        await Eventually.WithinAsync(
            () => Task.FromResult(_factory.TechnicalLogs.Logs.Any(l => l.CorrelationId == correlationId && l.Level == TechnicalLogLevel.Error)),
            Limit,
            ct);
        FastApiFactory.ReadLogText(logDirectory).ShouldNotContain("THROWINGKEY123");
        _factory.TechnicalLogs.Logs.Where(l => l.CorrelationId == correlationId).ShouldAllBe(l => !(l.Exception ?? string.Empty).Contains("THROWINGKEY123"));
    }

    /// <summary>Veritabanı sink'i yalnızca Warning+ ve dışlanmamış kategorileri yazar (döngü önlemi).</summary>
    /// <param name="level">Seviye.</param>
    /// <param name="category">Kategori.</param>
    /// <param name="expected">Yazılır mı.</param>
    [Theory]
    [InlineData(LogEventLevel.Information, "Konsept.Api", false)]
    [InlineData(LogEventLevel.Warning, "Konsept.Api", true)]
    [InlineData(LogEventLevel.Error, "Konsept.Api", true)]
    [InlineData(LogEventLevel.Fatal, "Konsept.Api", true)]
    [InlineData(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Database.Command", false)]
    [InlineData(LogEventLevel.Error, "Npgsql.Connection", false)]
    [InlineData(LogEventLevel.Warning, "Konsept.Modules.Monitoring.Infrastructure.Logging.DatabaseLogSink", false)]
    public void DatabaseSink_Filter_WarningPlusAndNotExcluded(LogEventLevel level, string category, bool expected)
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            null,
            new MessageTemplateParser().Parse("m"),
            [new LogEventProperty("SourceContext", new ScalarValue(category))]);

        DatabaseLogSink.ShouldPersist(logEvent).ShouldBe(expected);
    }

    /// <summary>
    /// Web log iletim ucu servis anahtarı ister: anahtarsız ve yanlış anahtarla 401; doğru anahtarla Warning+ kayıtlar
    /// kaynak Web olarak yazılır (gönderilen kaynak yok sayılır), Information yazılmaz, mesaj maskelidir; 200'den
    /// fazla kayıt 400.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task InternalLogs_WebForwarding_RequiresServiceKey()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");
        object[] body =
        [
            new { occurredAt = DateTimeOffset.UtcNow, level = 2, source = 1, message = $"bilgi {marker}" },
            new { occurredAt = DateTimeOffset.UtcNow, level = 3, source = 1, message = $"uyarı {marker} sk-proj-WEBFORWARDKEY12-w1x" },
            new { occurredAt = DateTimeOffset.UtcNow, level = 4, source = 2, message = $"hata {marker}", correlationId = "web-corr-1" },
        ];

        using var noKey = await client.PostAsJsonAsync("/api/internal/logs", body, ct);
        using var wrong = new HttpRequestMessage(HttpMethod.Post, "/api/internal/logs") { Content = JsonContent.Create(body) };
        wrong.Headers.Add(ServiceKeyHeader.Name, "yanlis-anahtar");
        using var wrongResponse = await client.SendAsync(wrong, ct);
        using var right = new HttpRequestMessage(HttpMethod.Post, "/api/internal/logs") { Content = JsonContent.Create(body) };
        right.Headers.Add(ServiceKeyHeader.Name, FastApiFactory.ServiceKey);
        using var rightResponse = await client.SendAsync(right, ct);
        using var tooMany = new HttpRequestMessage(HttpMethod.Post, "/api/internal/logs")
        {
            Content = JsonContent.Create(Enumerable.Repeat(body[1], 201).ToArray()),
        };
        tooMany.Headers.Add(ServiceKeyHeader.Name, FastApiFactory.ServiceKey);
        using var tooManyResponse = await client.SendAsync(tooMany, ct);

        noKey.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        wrongResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        rightResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await rightResponse.Content.ReadFromJsonAsync<int>(ct)).ShouldBe(2);
        tooManyResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var written = _factory.TechnicalLogs.Logs.Where(l => l.Message.Contains(marker, StringComparison.Ordinal)).ToList();
        written.Count.ShouldBe(2);
        written.ShouldAllBe(l => l.Source == LogSource.Web);
        written.ShouldNotContain(l => l.Message.StartsWith("bilgi", StringComparison.Ordinal));
        written.ShouldAllBe(l => !l.Message.Contains("WEBFORWARDKEY12"));
        written.Single(l => l.Level == TechnicalLogLevel.Error).CorrelationId.ShouldBe("web-corr-1");
    }

    /// <summary>
    /// Api'nin kendi yapılandırmasındaki sır (servis anahtarı) bilinen sır olarak kaydedilir: etiketsiz geçse de teknik
    /// loga açık yazılmaz (hunt r3, H3-H).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task InternalLogs_ConfiguredServiceKeyInMessage_NeverStored()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");
        object[] body = [new { occurredAt = DateTimeOffset.UtcNow, level = 3, source = 1, message = $"uyarı {marker} retry with {FastApiFactory.ServiceKey} failed" }];
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/logs") { Content = JsonContent.Create(body) };
        request.Headers.Add(ServiceKeyHeader.Name, FastApiFactory.ServiceKey);

        using var response = await client.SendAsync(request, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var written = _factory.TechnicalLogs.Logs.Single(l => l.Message.Contains(marker, StringComparison.Ordinal));
        written.Message.ShouldNotContain(FastApiFactory.ServiceKey);
        written.Message.ShouldContain("retry with ***");
    }

    /// <summary>
    /// Ajan özeti yalnızca kayıtlı bağlantıdan ve yalnızca Error/Critical seviyesinde kabul edilir; kabul edilen özet
    /// kaynak "ajan", ajan sürümü ve maskeli mesajla teknik loga düşer.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentHub_ReportLog_RejectsUnregisteredAndLowLevels()
    {
        var ct = TestContext.Current.CancellationToken;
        var marker = Guid.NewGuid().ToString("N");
        using var owner = await _factory.CreateAuthenticatedClientAsync(ct); // test kurulumu: eşleştirme (plan 0012)
        using var agentHttp = _factory.CreateClient();
        var agent = _factory.CreateAgentConnection(await AgentPairing.PairAsync(owner, agentHttp, _factory.Services, ct));
        await using (agent.ConfigureAwait(false))
        {
            await agent.StartAsync(ct);
            var unregistered = await agent.InvokeAsync<bool>(AgentHubContract.ReportLog, new AgentLogSummary(4, DateTimeOffset.UtcNow, $"kayıtsız {marker}", "0.2.0"), ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("TEST-PC", "0.2.0"), ct);
            var info = await agent.InvokeAsync<bool>(AgentHubContract.ReportLog, new AgentLogSummary(2, DateTimeOffset.UtcNow, $"bilgi {marker}", "0.2.0"), ct);
            var warning = await agent.InvokeAsync<bool>(AgentHubContract.ReportLog, new AgentLogSummary(3, DateTimeOffset.UtcNow, $"uyarı {marker}", "0.2.0"), ct);
            var error = await agent.InvokeAsync<bool>(
                AgentHubContract.ReportLog,
                new AgentLogSummary(4, DateTimeOffset.UtcNow, $"hata {marker} Password=agent-pw-77", "0.2.0"),
                ct);
            var critical = await agent.InvokeAsync<bool>(AgentHubContract.ReportLog, new AgentLogSummary(5, DateTimeOffset.UtcNow, $"kritik {marker}", "0.2.0"), ct);
            await agent.StopAsync(ct);

            unregistered.ShouldBeFalse();
            info.ShouldBeFalse();
            warning.ShouldBeFalse();
            error.ShouldBeTrue();
            critical.ShouldBeTrue();
        }

        var written = _factory.TechnicalLogs.Logs.Where(l => l.Message.Contains(marker, StringComparison.Ordinal)).ToList();
        written.Count.ShouldBe(2);
        written.ShouldAllBe(l => l.Source == LogSource.Agent && l.AgentVersion == "0.2.0");
        written.Select(l => l.Level).Order().ShouldBe([TechnicalLogLevel.Error, TechnicalLogLevel.Critical]);
        written.ShouldAllBe(l => !l.Message.Contains("agent-pw-77"));
    }

    /// <summary>Servis anahtarı yapılandırılmamışsa iç uç her isteği reddeder (kapalı varsayılan; review L-4).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task InternalLogs_NoConfiguredKey_AlwaysUnauthorized()
    {
        var ct = TestContext.Current.CancellationToken;
        var unconfigured = _factory.WithWebHostBuilder(b => b
            .UseSetting("Internal:ServiceKey", string.Empty)
            .UseSetting("Logging:Directory", Path.Combine(_factory.LogDirectory, "nokey-" + Guid.NewGuid().ToString("N"))));
        using var client = unconfigured.CreateClient();
        using var withKey = new HttpRequestMessage(HttpMethod.Post, "/api/internal/logs") { Content = JsonContent.Create(Array.Empty<object>()) };
        withKey.Headers.Add(ServiceKeyHeader.Name, FastApiFactory.ServiceKey);
        using var empty = new HttpRequestMessage(HttpMethod.Post, "/api/internal/logs") { Content = JsonContent.Create(Array.Empty<object>()) };
        empty.Headers.Add(ServiceKeyHeader.Name, string.Empty);

        (await client.SendAsync(withKey, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.SendAsync(empty, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// AC-8 veritabanı yolu, tam alan tablosu (review round 1, M-3): adlandırılmış gizli alan içeren Warning veritabanı
    /// teknik loguna düşer ama değer düz metin değildir.
    /// </summary>
    /// <param name="field">Gizli alan adı.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [MemberData(nameof(SecretFields))]
    public async Task Masking_EverySecretField_NeverPlainInDatabase(string field)
    {
        var ct = TestContext.Current.CancellationToken;
        var sample = SecretFieldSamples.Get(field);
        using var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");
        var logger = _factory.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("Konsept.Api.Tests.Masking");

#pragma warning disable CA1848, CA2254 // Test: alan adı tablodan geliyor.
        Microsoft.Extensions.Logging.LoggerExtensions.LogWarning(logger, $"gizli {{Marker}} {{{field}}}", marker, sample.Value);
#pragma warning restore CA1848, CA2254

        await Eventually.WithinAsync(() => Task.FromResult(_factory.TechnicalLogs.Logs.Any(l => l.Message.Contains(marker, StringComparison.Ordinal))), Limit, ct);
        var log = _factory.TechnicalLogs.Logs.Single(l => l.Message.Contains(marker, StringComparison.Ordinal));
        log.Message.ShouldNotContain(sample.Secret);
    }
}
