using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Application.Concrete;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Gerçek ajan bileşenleri (oturum akışı, HTTP eşleştirme istemcisi, SignalR bağlantısı, dosya belirteç deposu) ↔ gerçek
/// Kestrel üzerindeki API (spec 0012 AC-1, AC-3, AC-8, AC-9, AC-15 uçtan uca): kod → onay → "Eşleşti" ve sitede "Ajan bağlı";
/// web'den çıkış → "Oturum kapandı" ve yeni kod; sunucu yokken "ulaşılamıyor", gelince kod; eşleşmiş ajan API yeniden açılınca
/// yeniden eşleştirmesiz bağlanır.
/// </summary>
public sealed class AgentPairingEndToEndTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Eşleşmemiş ajan <c>ABCD-1234</c> biçiminde 10 dakikalık kod gösterir; kullanıcı onaylayınca ajan "Eşleşti" olur ve sitede en
    /// geç 10 sn'de "Ajan bağlı" görünür; web'den çıkış en geç 10 sn'de ajanı "Oturum kapandı"ya düşürür, belirteç silinir ve
    /// yeni kod gösterilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RealAgent_PairApproveLogout_ShowsPairedThenSessionClosedAndNewCode()
    {
        var ct = TestContext.Current.CancellationToken;
        var port = FreeLoopbackPort();
        var api = new KestrelApiFactory(port);
        var directory = Directory.CreateTempSubdirectory("anewconcept-agent-");
        await using (api.ConfigureAwait(false))
        {
            api.StartServer();
            var (session, connection, pairing, tokens) = CreateAgent(port, directory.FullName);
            await using (connection.ConfigureAwait(false))
            {
                using (pairing)
                {
                    await session.StartAsync(ct);
                    await Eventually.WithinAsync(() => Task.FromResult(session.Current.Code is not null), Limit, ct);
                    var code = session.Current.Code!;
                    code.ShouldMatch("^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$");
                    (session.Current.ExpiresAt!.Value - DateTimeOffset.UtcNow).ShouldBeInRange(TimeSpan.FromMinutes(9.5), TimeSpan.FromMinutes(10));
                    session.Current.State.ShouldBe(AgentSessionState.Unpaired);

                    using var user = api.CreateClient();
                    using (var approve = await AgentPairing.DecideAsync(user, api.Services, "approve", code, ct))
                    {
                        approve.StatusCode.ShouldBe(HttpStatusCode.OK);
                    }

                    await Eventually.WithinAsync(() => Task.FromResult(session.Current.State == AgentSessionState.Paired), Limit, ct);
                    await Eventually.WithinAsync(() => IsConnectedAsync(user, ct), Limit, ct);
                    tokens.Read().ShouldNotBeNull();

                    using (var logout = await user.PostAsync(new Uri("/api/auth/logout", UriKind.Relative), null, ct))
                    {
                        logout.StatusCode.ShouldBe(HttpStatusCode.OK);
                    }

                    await Eventually.WithinAsync(
                        () => Task.FromResult(session.Current.State == AgentSessionState.SessionClosed && session.Current.Code is { } next && next != code),
                        Limit,
                        ct);
                    tokens.Read().ShouldBeNull();
                    await session.StopAsync(ct);
                }
            }
        }

        directory.Delete(recursive: true);
    }

    /// <summary>
    /// Ajan sunucuya ulaşamazken açılınca "Sunucuya ulaşılamıyor" gösterir ve kendiliğinden dener; sunucu gelince kod gösterir.
    /// Eşleşmiş ajan sunucu kapanınca yine "ulaşılamıyor" gösterir; sunucu (aynı veriyle) yeniden açılınca eşleştirme istemeden
    /// bağlanır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RealAgent_ServerDownAtStart_ShowsUnreachableThenCode()
    {
        var ct = TestContext.Current.CancellationToken;
        var port = FreeLoopbackPort();
        var stores = new KestrelApiStores();
        var directory = Directory.CreateTempSubdirectory("anewconcept-agent-");
        var (session, connection, pairing, tokens) = CreateAgent(port, directory.FullName);
        await using (connection.ConfigureAwait(false))
        {
            using (pairing)
            {
                await session.StartAsync(ct);
                await Eventually.WithinAsync(() => Task.FromResult(session.Current.State == AgentSessionState.Unreachable), Limit, ct);

                var first = new KestrelApiFactory(port, stores: stores);
                first.StartServer();
                await Eventually.WithinAsync(() => Task.FromResult(session.Current.Code is not null), Limit, ct);
                using (var user = first.CreateClient())
                using (var approve = await AgentPairing.DecideAsync(user, first.Services, "approve", session.Current.Code!, ct))
                {
                    approve.StatusCode.ShouldBe(HttpStatusCode.OK);
                }

                await Eventually.WithinAsync(() => Task.FromResult(session.Current.State == AgentSessionState.Paired), Limit, ct);
                var token = tokens.Read();

                await first.DisposeAsync();
                await Eventually.WithinAsync(() => Task.FromResult(session.Current.State == AgentSessionState.Unreachable), Limit, ct);

                var second = new KestrelApiFactory(port, stores: stores);
                await using (second.ConfigureAwait(false))
                {
                    second.StartServer();
                    await Eventually.WithinAsync(() => Task.FromResult(session.Current.State == AgentSessionState.Paired), Limit, ct);
                    tokens.Read().ShouldBe(token);
                    session.Current.Code.ShouldBeNull();
                }

                await session.StopAsync(ct);
            }
        }

        directory.Delete(recursive: true);
    }

    private static (AgentSessionCoordinator Session, SignalRServerConnection Connection, HttpPairingClient Pairing, FileAgentTokenStore Tokens) CreateAgent(int port, string directory)
    {
        var settings = new AgentSettings(new Uri($"http://127.0.0.1:{port}/"), new Uri("http://127.0.0.1/"), TimeSpan.FromMilliseconds(300));
        var tokens = new FileAgentTokenStore(directory);
        var info = new TestAgentInfo();
        var connection = new SignalRServerConnection(settings, info, accessToken: tokens.Read);
        var pairing = new HttpPairingClient(settings);
        var session = new AgentSessionCoordinator(
            connection, pairing, tokens, info, new AgentSessionOptions(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300)), TimeProvider.System);
        return (session, connection, pairing, tokens);
    }

    private static async Task<bool> IsConnectedAsync(HttpClient client, CancellationToken ct)
    {
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
