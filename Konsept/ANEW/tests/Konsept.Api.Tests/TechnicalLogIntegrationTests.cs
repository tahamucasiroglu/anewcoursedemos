using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Logging;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Veritabanındaki teknik log, gerçek PostgreSQL 17 ile (full profil): Warning+ yazılır / Information yazılmaz,
/// 30 gün saklama (AC-6), ajanın Error/Critical özeti gerçek SignalR istemcisiyle kaynak "ajan" olarak (AC-7), gizli
/// alanlar veritabanında düz metin değil (AC-8, veritabanı kısmı).
/// </summary>
[Trait("Category", "Integration")]
public sealed class TechnicalLogIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(15);
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="TechnicalLogIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public TechnicalLogIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Api'nin Warning, Error ve Critical logları veritabanına yazılır; Information yazılmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task DatabaseSink_WarningPlus_PersistedInformationNot()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");
        var logger = _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Api.Tests.Marker");

#pragma warning disable CA1848, CA2254 // Test: farklı seviyelerde tek tek yazılıyor.
        logger.LogInformation("bilgi {Marker}", marker);
        logger.LogWarning("uyarı {Marker}", marker);
        logger.LogError("hata {Marker}", marker);
        logger.LogCritical("kritik {Marker}", marker);
#pragma warning restore CA1848, CA2254

        await Eventually.WithinAsync(async () => await CountAsync($"\"Message\" LIKE '%{marker}%'") == 3, Limit, ct);
        (await CountAsync($"\"Message\" = 'uyarı {marker}'")).ShouldBe(1);
        (await CountAsync($"\"Message\" LIKE '%bilgi%{marker}%'")).ShouldBe(0);
        (await CountAsync($"\"Message\" LIKE '%{marker}%' AND \"Source\" = {(int)LogSource.Api}")).ShouldBe(3);
    }

    /// <summary>30 günlük teknik log kaydı korunur, 31 günlük olan silinir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TechnicalLogRetention_Keeps30_Deletes31()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ITechnicalLogRepository>().AddRangeAsync(
                [
                    TechnicalLog.Create(now.AddDays(-30).AddMinutes(5), TechnicalLogLevel.Warning, LogSource.Api, $"otuz {marker}"),
                    TechnicalLog.Create(now.AddDays(-31), TechnicalLogLevel.Warning, LogSource.Api, $"otuzbir {marker}"),
                ],
                ct);
            await scope.ServiceProvider.GetRequiredService<ITechnicalLogService>().PurgeExpiredAsync(ct);
        }

        (await CountAsync($"\"Message\" = 'otuz {marker}'")).ShouldBe(1);
        (await CountAsync($"\"Message\" = 'otuzbir {marker}'")).ShouldBe(0);
    }

    /// <summary>Gizli alanlar veritabanındaki teknik logda ve olay kaydında düz metin olarak görünmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Masking_NamedSecretFields_NeverPlainInDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");
        var logger = _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Api.Tests.Masking");

#pragma warning disable CA1848, CA2254 // Test: gizli alanlar adlandırılmış özellik olarak.
        logger.LogWarning("gizli {Marker} {Password} {ApiKey} {RefreshToken} {ConnectionString}", marker, "db-hunter2", "sk-proj-DBMASKKEY1234-d4b", "refresh-opaque-9", "Host=x;Password=db-conn-pw");
#pragma warning restore CA1848, CA2254
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISystemEventWriter>().WriteAsync(
                new SystemEventEntry("test.secret", SystemEventLevel.Warning, $"olay {marker} sk-ant-api03-EVENTKEY12345-e5f"),
                ct);
        }

        await Eventually.WithinAsync(async () => await CountAsync($"\"Message\" LIKE '%gizli {marker}%'") == 1, Limit, ct);
        foreach (var secret in new[] { "db-hunter2", "DBMASKKEY1234", "refresh-opaque-9", "db-conn-pw", "EVENTKEY12345" })
        {
            (await CountAsync($"\"Message\" LIKE '%{secret}%' OR \"Exception\" LIKE '%{secret}%'")).ShouldBe(0);
            Convert.ToInt64(await _factory.Database.ScalarAsync($"SELECT count(*) FROM monitoring.system_events WHERE \"Message\" LIKE '%{secret}%'"), CultureInfo.InvariantCulture).ShouldBe(0);
        }

        (await CountAsync($"\"Message\" LIKE '%gizli {marker}%sk-…d4b%'")).ShouldBe(1);
    }

    /// <summary>
    /// Ajan (gerçek SignalR istemcisi ve ajan log kurulumu) API'ye bağlıyken oluşan Error ve Critical logları maskeli
    /// özet olarak kaynak "ajan" ve ajan sürümüyle veritabanına düşer; Information ve Warning düşmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentErrorSummary_ReachesDatabaseAsAgentSource()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = new PostgresTestDatabase();
        await database.InitializeAsync();
        var agentDir = Directory.CreateTempSubdirectory("anewconcept-agent-");
        try
        {
            var port = FreeLoopbackPort();
            var api = new KestrelApiFactory(port, databaseConnectionString: database.ConnectionString);
            await using (api.ConfigureAwait(false))
            {
                api.StartServer();
                var logging = AgentLogging.Create(agentDir.FullName, TimeProvider.System);
                var settings = new AgentSettings(new Uri($"http://127.0.0.1:{port}/"), new Uri("http://127.0.0.1/"), TimeSpan.FromMilliseconds(300));
                var token = await api.PairAgentAsync(ct); // test kurulumu (plan 0012)
                var connection = new SignalRServerConnection(settings, new TestAgentInfo(), logger: logging.Logger, accessToken: () => token);
                logging.SummarySink.Attach(connection);
                await using (connection.ConfigureAwait(false))
                {
                    await connection.StartAsync(ct);
                    await Eventually.WithinAsync(() => Task.FromResult(connection.State == ConnectionState.Connected), Limit, ct);
                    var marker = Guid.NewGuid().ToString("N");

                    logging.Logger.Information("ajan bilgi {Marker}", marker);
                    logging.Logger.Warning("ajan uyarı {Marker}", marker);
                    logging.Logger.Error(new InvalidOperationException("Password=agent-secret-1"), "ajan hata {Marker} {ApiKey}", marker, "sk-proj-AGENTKEY12345-a6g");
                    logging.Logger.Fatal("ajan kritik {Marker}", marker);

                    await Eventually.WithinAsync(
                        async () => Convert.ToInt64(await database.ScalarAsync($"SELECT count(*) FROM monitoring.technical_logs WHERE \"Message\" LIKE '%{marker}%'"), CultureInfo.InvariantCulture) == 2,
                        Limit,
                        ct);
                    Convert.ToInt64(
                        await database.ScalarAsync($"SELECT count(*) FROM monitoring.technical_logs WHERE \"Message\" LIKE '%{marker}%' AND \"Source\" = {(int)LogSource.Agent} AND \"AgentVersion\" = '0.1.0'"),
                        CultureInfo.InvariantCulture).ShouldBe(2);
                    Convert.ToInt64(
                        await database.ScalarAsync($"SELECT count(*) FROM monitoring.technical_logs WHERE \"Message\" LIKE '%AGENTKEY12345%' OR \"Message\" LIKE '%agent-secret-1%'"),
                        CultureInfo.InvariantCulture).ShouldBe(0);
                    await connection.StopAsync(ct);
                }

                logging.Dispose();
            }
        }
        finally
        {
            await database.DisposeAsync();
            agentDir.Delete(recursive: true);
        }
    }

    private static int FreeLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private async Task<long> CountAsync(string where) =>
        Convert.ToInt64(await _factory.Database.ScalarAsync($"SELECT count(*) FROM monitoring.technical_logs WHERE {where}"), CultureInfo.InvariantCulture);
}
