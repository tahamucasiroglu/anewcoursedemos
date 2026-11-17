using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Eksik bileşen akışının ajan ↔ API kanalı (spec 0027 AC-7): gerçek ajan istemcisi ↔ gerçek Kestrel'deki API. Araç denetimi eksiklerden
/// ajanın kurabildiklerini bildirir; kurulum komutu ajanın kurucusuna gider; kurulumdan sonra ve istenince kurulu bileşen listesi sunucuya
/// yeniden bildirilir (B-149).
/// </summary>
public sealed class AgentInstallChannelTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    /// <summary>CheckTools kurulabilir bilgisini taşır; InstallComponents kurucuya gider; kurulum sonrası liste sunucuda güncellenir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task CheckInstallReport_EndToEnd()
    {
        var ct = TestContext.Current.CancellationToken;
        var port = FreeLoopbackPort();
        var settings = new AgentSettings(new Uri($"http://127.0.0.1:{port}/"), new Uri("http://127.0.0.1/"), TimeSpan.FromMilliseconds(300));
        var api = new KestrelApiFactory(port);
        await using (api.ConfigureAwait(false))
        {
            api.StartServer();
            var token = await api.PairAgentAsync(ct);
            var stages = new RecordingStageRunner { Missing = ["hyperframes", "supertonic-3"] };
            var installed = new List<string> { "node" };
            var installer = new FakeComponentInstaller([new AgentComponentInfo("hyperframes", "HyperFrames", 40, 115)], installed);
            var connection = new SignalRServerConnection(
                settings, new TestAgentInfo(), accessToken: () => token, stages: stages, installedComponents: () => [.. installed], installer: installer);
            await using (connection.ConfigureAwait(false))
            {
                await connection.StartAsync(ct);
                await Eventually.WithinAsync(() => Task.FromResult(connection.State == ConnectionState.Connected), Limit, ct);
                using var client = api.CreateClient();
                var user = UserOf(client);
                var channel = api.Services.GetRequiredService<IAgentChannel>();
                var components = api.Services.GetRequiredService<IInstalledComponentsReader>();
                var session = api.Services.GetRequiredService<IAgentConnectionRegistry>().Find(user)!;
                components.Get(user).Order(StringComparer.Ordinal).ShouldBe(["node"]);

                var check = await channel.CheckToolsAsync(session.ConnectionId, new AgentToolCheckRequest(["node", "hyperframes", "supertonic-3"]), ct);
                check.Missing.ShouldBe(["hyperframes", "supertonic-3"]);
                check.Installable.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBe(new AgentComponentInfo("hyperframes", "HyperFrames", 40, 115));

                var reply = await channel.InstallComponentsAsync(session.ConnectionId, new AgentInstallRequest(["hyperframes"]), ct);

                (reply.Ok, reply.Failed.Count).ShouldBe((true, 0));
                installer.Requests.ShouldHaveSingleItem().ShouldBe(["hyperframes"]);
                await Eventually.WithinAsync(() => Task.FromResult(components.Get(user).Contains("hyperframes")), Limit, ct);

                installed.Add("remotion");
                (await connection.TryReportComponentsAsync(ct)).ShouldBeTrue();
                components.Get(user).Order(StringComparer.Ordinal).ShouldBe(["hyperframes", "node", "remotion"]);
                await connection.StopAsync(ct);
            }
        }
    }

    // Fabrikanın kullanıcısı: istemcinin JWT'sindeki "sub" (yalnızca test okuması).
    private static Guid UserOf(HttpClient client)
    {
        var payload = client.DefaultRequestHeaders.Authorization!.Parameter!.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
        return json.RootElement.GetProperty("sub").GetGuid();
    }

    private static int FreeLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
