using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Sürüm kapısı uçtan uca (spec 0023 AC-6): gerçek ajan istemcisi (sürüm 0.1.0) ↔ gerçek Kestrel'deki API (ürün sürümü farklı, kapı açık).
/// Kayıt cevabı "güncelleme gerekli" der; durum alanı "Ajan güncellemesi gerekli", "Ajanı test et" ajana gitmez; ajan kapıyı atlayan bir
/// komutu bile yürütmez (çift güvence); güncellemeye başlama (BeginUpdate) "Üretiliyor" iş yokken kabul edilir.
/// </summary>
public sealed class AgentUpdateGateTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    /// <summary>Uyumsuz ajan bağlanır ama komut almaz; web durumu ve test et "güncelleme gerekli" der.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_Mismatch_ReplyUpdateRequired_NoCommands()
    {
        var ct = TestContext.Current.CancellationToken;
        var port = FreeLoopbackPort();
        var settings = new AgentSettings(new Uri($"http://127.0.0.1:{port}/"), new Uri("http://127.0.0.1/"), TimeSpan.FromMilliseconds(300));
        var api = new KestrelApiFactory(port, requireSameVersion: true);
        await using (api.ConfigureAwait(false))
        {
            api.StartServer();
            var token = await api.PairAgentAsync(ct);
            var stages = new RecordingStageRunner();
            var required = new List<string>();
            var connection = new SignalRServerConnection(settings, new TestAgentInfo(), accessToken: () => token, stages: stages);
            connection.UpdateRequiredReceived += (_, version) => required.Add(version);
            await using (connection.ConfigureAwait(false))
            {
                await connection.StartAsync(ct);
                await Eventually.WithinAsync(() => Task.FromResult(connection.State == ConnectionState.Connected), Limit, ct);

                connection.UpdateRequired.ShouldBeTrue();
                required.Count.ShouldBe(1);
                required[0].ShouldNotBe("0.1.0");

                using var client = api.CreateClient();
                var status = await client.GetFromJsonAsync<JsonElement>("/api/system/status", ct);
                status.GetProperty("agent").GetProperty("isConnected").GetBoolean().ShouldBeFalse();
                status.GetProperty("agent").GetProperty("updateRequired").GetBoolean().ShouldBeTrue();
                using var test = await client.PostAsync(new Uri("/api/agent/test", UriKind.Relative), null, ct);
                test.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
                (await test.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("code").GetString().ShouldBe("agent.update_required");

                // Kapıyı atlayan doğrudan bir komut bile ajanda yürütülmez (ajan tarafı kapı).
                var session = api.Services.GetRequiredService<IAgentConnectionRegistry>().Find(UserOf(client))!;
                var reply = await api.Services.GetRequiredService<IAgentChannel>().RunStageAsync(
                    session.ConnectionId,
                    new AgentStageCommand { JobId = Guid.NewGuid(), Stage = AgentStageCommand.Render, Engine = "hyperframes", Language = "tr", Format = "landscape" },
                    ct);
                reply.Ok.ShouldBeFalse();
                stages.Executed.ShouldBe(0);

                (await connection.TryBeginUpdateAsync(ct)).ShouldBe(true);
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
