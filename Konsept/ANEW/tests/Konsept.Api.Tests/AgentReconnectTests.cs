using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Ajanın gerçek SignalR istemcisi ↔ gerçek Kestrel üzerindeki API (spec 0001 AC-6): ajan kendiliğinden
/// bağlanır ve API yeniden başlatılınca kendiliğinden yeniden bağlanıp kaydolur.
/// </summary>
public sealed class AgentReconnectTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    /// <summary>API kapanıp aynı adreste yeniden açılınca ajan en geç 10 sn'de yeniden "bağlı" görünür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentClient_ServerRestart_Reconnects()
    {
        var ct = TestContext.Current.CancellationToken;
        var port = FreeLoopbackPort();
        var settings = new AgentSettings(new Uri($"http://127.0.0.1:{port}/"), new Uri("http://127.0.0.1/"), TimeSpan.FromMilliseconds(300));
        var connection = new SignalRServerConnection(settings, new TestAgentInfo());
        await using (connection.ConfigureAwait(false))
        {
            var first = new KestrelApiFactory(port);
            first.StartServer();
            await connection.StartAsync(ct);
            await Eventually.WithinAsync(() => IsConnectedAsync(first, ct), Limit, ct);
            connection.State.ShouldBe(ConnectionState.Connected);

            await first.DisposeAsync();
            await Eventually.WithinAsync(() => Task.FromResult(connection.State != ConnectionState.Connected), Limit, ct);

            var second = new KestrelApiFactory(port);
            await using (second.ConfigureAwait(false))
            {
                second.StartServer();
                await Eventually.WithinAsync(() => IsConnectedAsync(second, ct), Limit, ct);
                connection.State.ShouldBe(ConnectionState.Connected);
            }

            await connection.StopAsync(ct);
        }
    }

    private static async Task<bool> IsConnectedAsync(KestrelApiFactory factory, CancellationToken ct)
    {
        using var client = factory.CreateClient();
        var status = await client.GetFromJsonAsync<JsonElement>("/api/system/status", ct);
        return status.GetProperty("agent").GetProperty("isConnected").GetBoolean();
    }

    private static int FreeLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
