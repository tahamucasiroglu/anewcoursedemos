using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Ajan ↔ API yolu uçtan uca (gerçek SignalR, test sunucusu): bağlanma/kopma durumu (AC-6),
/// "Ajanı test et" (AC-7) ve olay kaydı (AC-8, hızlı profil).
/// </summary>
public sealed class AgentHubApiTests : IClassFixture<FastApiFactory>
{
    private static readonly TimeSpan StatusLimit = TimeSpan.FromSeconds(10);
    private readonly FastApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AgentHubApiTests"/> class.</summary>
    /// <param name="factory">Hızlı API fabrikası.</param>
    public AgentHubApiTests(FastApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Ajan bağlanınca en geç 10 sn'de "bağlı", kapanınca en geç 10 sn'de "bağlı değil" görünür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentConnection_ConnectDisconnect_StatusWithin10s()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateAuthenticatedClientAsync(ct);
        var agent = _factory.CreateAgentConnection();
        await using (agent.ConfigureAwait(false))
        {
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("TAHA-PC", "0.1.0"), ct);

            await Eventually.WithinAsync(async () => await IsAgentConnectedAsync(client, ct), StatusLimit, ct);

            await agent.StopAsync(ct);
            await Eventually.WithinAsync(async () => !await IsAgentConnectedAsync(client, ct), StatusLimit, ct);
        }

        _factory.Events.Events.ShouldContain(e => e.Type == "agent.connected");
        _factory.Events.Events.ShouldContain(e => e.Type == "agent.disconnected");
    }

    /// <summary>"Ajanı test et" komutu ajana ulaşır; ajanın versiyonu ve makine adı döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TestAgent_ConnectedAgentAnswers_ReturnsVersionAndMachine()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateAuthenticatedClientAsync(ct);
        var agent = _factory.CreateAgentConnection();
        await using (agent.ConfigureAwait(false))
        {
            agent.On(AgentHubContract.Ping, () => new AgentPingReply("TAHA-PC", "0.1.0"));
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("TAHA-PC", "0.1.0"), ct);

            using var response = await client.PostAsync(new Uri("/api/agent/test", UriKind.Relative), null, ct);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            body.GetProperty("machineName").GetString().ShouldBe("TAHA-PC");
            body.GetProperty("agentVersion").GetString().ShouldBe("0.1.0");
        }
    }

    /// <summary>Ajan bağlı değilken 503 ve "Ajan bağlı değil" mesajı döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TestAgent_NoAgent_ReturnsNotConnectedProblem()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateAuthenticatedClientAsync(ct);
        await Eventually.WithinAsync(async () => !await IsAgentConnectedAsync(client, ct), StatusLimit, ct);

        using var response = await client.PostAsync(new Uri("/api/agent/test", UriKind.Relative), null, ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        body.GetProperty("title").GetString().ShouldBe("Ajan bağlı değil");
        body.GetProperty("code").GetString().ShouldBe("agent.not_connected");
    }

    private static async Task<bool> IsAgentConnectedAsync(HttpClient client, CancellationToken ct)
    {
        var status = await client.GetFromJsonAsync<JsonElement>("/api/system/status", ct);
        return status.GetProperty("agent").GetProperty("isConnected").GetBoolean();
    }
}
