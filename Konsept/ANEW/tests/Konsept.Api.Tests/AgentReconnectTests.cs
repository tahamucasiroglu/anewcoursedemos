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

    /// <summary>
    /// Veritabanı kapalıyken (olay yazılamaz) ajan yine kaydolur, "bağlı" kalır ve bağlanma döngüsüne
    /// girmez (review round 1, M-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentClient_DatabaseDown_StaysConnected()
    {
        var ct = TestContext.Current.CancellationToken;
        var port = FreeLoopbackPort();
        var settings = new AgentSettings(new Uri($"http://127.0.0.1:{port}/"), new Uri("http://127.0.0.1/"), TimeSpan.FromMilliseconds(300));
        var api = new KestrelApiFactory(port, databaseDown: true);
        await using (api.ConfigureAwait(false))
        {
            api.StartServer();
            var connection = new SignalRServerConnection(settings, new TestAgentInfo());
            await using (connection.ConfigureAwait(false))
            {
                var changes = 0;
                await connection.StartAsync(ct);
                await Eventually.WithinAsync(() => Task.FromResult(connection.State == ConnectionState.Connected), Limit, ct);
                connection.StateChanged += (_, _) => Interlocked.Increment(ref changes);

                await Task.Delay(TimeSpan.FromSeconds(2), ct);

                connection.State.ShouldBe(ConnectionState.Connected);
                changes.ShouldBe(0);
                (await IsConnectedAsync(api, ct)).ShouldBeTrue();
                await connection.StopAsync(ct);
            }
        }
    }

    /// <summary>
    /// Sunucu kaydı geçici olarak reddederse istemci hub'ı yeniden başlatmadan (aynı bağlantıda) yalnızca
    /// kaydı yeniden dener ve sonunda "bağlı" olur (review round 2, L-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentClient_RegisterRejected_RetriesRegistrationOnSameConnection()
    {
        var ct = TestContext.Current.CancellationToken;
        var port = FreeLoopbackPort();
        var flaky = new FlakyRegistrationState(failuresBeforeSuccess: 2);
        var settings = new AgentSettings(new Uri($"http://127.0.0.1:{port}/"), new Uri("http://127.0.0.1/"), TimeSpan.FromMilliseconds(200));
        var api = new KestrelApiFactory(port, flakyRegistration: flaky);
        await using (api.ConfigureAwait(false))
        {
            api.StartServer();
            var connection = new SignalRServerConnection(settings, new TestAgentInfo());
            await using (connection.ConfigureAwait(false))
            {
                await connection.StartAsync(ct);
                await Eventually.WithinAsync(() => IsConnectedAsync(api, ct), Limit, ct);

                connection.State.ShouldBe(ConnectionState.Connected);
                flaky.Attempts.ShouldBe(3);
                flaky.ConnectionIds.Distinct().Count().ShouldBe(1);
                await connection.StopAsync(ct);
            }
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
