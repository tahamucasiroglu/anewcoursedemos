using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Gerçek PostgreSQL 17 ile (full profil): boş veritabanında şemanın açılışta oluşması ve ajan
/// olaylarının kalıcı kaydı + Günlük şeridindeki son olay (spec 0001 AC-8).
/// </summary>
[Trait("Category", "Integration")]
public sealed class DatabaseIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="DatabaseIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public DatabaseIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Boş veritabanıyla başlayan API, monitoring şemasını ve olay tablosunu kendisi oluşturur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Api_EmptyDatabase_CreatesSchemaOnStartup()
    {
        using var client = _factory.CreateClient();
        var health = await client.GetFromJsonAsync<JsonElement>("/health", TestContext.Current.CancellationToken);

        var tableCount = await _factory.Database.ScalarAsync(
            "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'monitoring' AND table_name = 'system_events'");

        health.GetProperty("database").GetString().ShouldBe("ok");
        Convert.ToInt64(tableCount, System.Globalization.CultureInfo.InvariantCulture).ShouldBe(1);
    }

    /// <summary>Ajan bağlanma/kopma olayları veritabanına yazılır; durumdaki son olay Günlük şeridine gider.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentEvents_Persisted_LastEventShownInStrip()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var agent = _factory.CreateAgentConnection();
        await using (agent.ConfigureAwait(false))
        {
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("TAHA-PC", "0.1.0"), ct);

            var status = await client.GetFromJsonAsync<JsonElement>("/api/system/status", ct);
            var lastEvent = status.GetProperty("lastEvent");
            lastEvent.GetProperty("type").GetString().ShouldBe("agent.connected");
            lastEvent.GetProperty("message").GetString().ShouldBe("Ajan bağlandı: TAHA-PC (v0.1.0)");

            await agent.StopAsync(ct);
            await Eventually.WithinAsync(
                async () => Convert.ToInt64(
                    await _factory.Database.ScalarAsync("SELECT count(*) FROM monitoring.system_events WHERE \"Type\" = 'agent.disconnected'"),
                    System.Globalization.CultureInfo.InvariantCulture) == 1,
                TimeSpan.FromSeconds(10),
                ct);
        }

        var connected = await _factory.Database.ScalarAsync(
            "SELECT count(*) FROM monitoring.system_events WHERE \"Type\" = 'agent.connected' AND \"UserId\" IS NULL");
        Convert.ToInt64(connected, System.Globalization.CultureInfo.InvariantCulture).ShouldBe(1);
    }
}
