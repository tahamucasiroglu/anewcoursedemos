using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Ajan oturumu, sahiplik ve iptal, hızlı profil (spec 0012 AC-5, AC-6, AC-7, AC-8, AC-12, AC-13 Api tarafı; gerçek SignalR,
/// test sunucusu): eşleşmemiş/iptal edilmiş ajan bağlanamaz; kullanıcı yalnızca kendi ajanını görür ve test eder; yeni eşleştirme
/// ve çıkış eski ajanın bağlantısını en geç 10 sn'de kapatır; loglarda ve olaylarda sır/belirteç yoktur.
/// </summary>
public sealed class AgentOwnershipApiTests : IClassFixture<FastApiFactory>
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);
    private readonly FastApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AgentOwnershipApiTests"/> class.</summary>
    /// <param name="factory">Hızlı API fabrikası.</param>
    public AgentOwnershipApiTests(FastApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Belirteçsiz, uydurma ya da iptal edilmiş (çıkış sonrası) belirteçle hub'a bağlanılamaz (401); bu bağlantılar komut alamaz
    /// ve kimse için "bağlı" görünmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Hub_UnpairedOrRevokedToken_Rejected_NoCommand()
    {
        var ct = TestContext.Current.CancellationToken;
        using var user = await _factory.CreateAuthenticatedClientAsync(ct);
        var token = await PairAsync(user, "IPTAL-PC", ct);
        using (var logout = await user.PostAsync(new Uri("/api/auth/logout", UriKind.Relative), null, ct))
        {
            logout.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        foreach (var candidate in new string?[] { null, "knr_uydurma-belirtec-0000000000000000000000000", token })
        {
            var agent = _factory.CreateAgentConnection(candidate);
            await using (agent.ConfigureAwait(false))
            {
                var pinged = false;
                agent.On(AgentHubContract.Ping, () =>
                {
                    pinged = true;
                    return new AgentPingReply("IPTAL-PC", "0.5.0");
                });

                var error = await Should.ThrowAsync<HttpRequestException>(() => agent.StartAsync(ct));

                error.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
                agent.State.ShouldBe(HubConnectionState.Disconnected);
                pinged.ShouldBeFalse();
            }
        }
    }

    /// <summary>
    /// A'nın ajanı bağlıyken B "Ajan bağlı değil" görür, B'nin "Ajanı test et"i A'nın ajanına ulaşmaz, B'nin Günlük şeridinde A'nın
    /// ajan olayları yoktur; A kendi ajanını görür, test eder ve olaylarını görür.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TwoUsers_StatusTestAndJournal_OnlyOwn()
    {
        var ct = TestContext.Current.CancellationToken;
        using var userA = await _factory.CreateAuthenticatedClientAsync(ct);
        using var userB = await _factory.CreateAuthenticatedClientAsync(ct);
        var agent = _factory.CreateAgentConnection(await PairAsync(userA, "SAHIP-A-PC", ct));
        await using (agent.ConfigureAwait(false))
        {
            agent.On(AgentHubContract.Ping, () => new AgentPingReply("SAHIP-A-PC", "0.5.0"));
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("SAHIP-A-PC", "0.5.0"), ct);

            var statusA = await StatusAsync(userA, ct);
            var statusB = await StatusAsync(userB, ct);
            using var testA = await userA.PostAsync(new Uri("/api/agent/test", UriKind.Relative), null, ct);
            using var testB = await userB.PostAsync(new Uri("/api/agent/test", UriKind.Relative), null, ct);
            var testBBody = await testB.Content.ReadFromJsonAsync<JsonElement>(ct);

            statusA.GetProperty("agent").GetProperty("isConnected").GetBoolean().ShouldBeTrue();
            statusA.GetProperty("lastEvent").GetProperty("message").GetString().ShouldBe("Ajan bağlandı: SAHIP-A-PC (v0.5.0)");
            statusB.GetProperty("agent").GetProperty("isConnected").GetBoolean().ShouldBeFalse();
            statusB.GetProperty("lastEvent").ToString().ShouldNotContain("SAHIP-A-PC");
            testA.StatusCode.ShouldBe(HttpStatusCode.OK);
            testB.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            testBBody.GetProperty("title").GetString().ShouldBe("Ajan bağlı değil");
            _factory.Events.Events.Where(e => e.Message.Contains("SAHIP-A-PC", StringComparison.Ordinal))
                .ShouldAllBe(e => e.UserId != null);
        }
    }

    /// <summary>
    /// Eşleşmiş ajanı olan kullanıcı ikinci ajanı eşleştirince eski ajan en geç 10 sn'de "oturum kapandı" bildirimini alır ve
    /// bağlantısı kapanır; eski belirteçle yeniden bağlanamaz; durumda yalnızca yeni ajan görünür.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SecondPairing_RevokesPrevious_ClosedWithin10s()
    {
        var ct = TestContext.Current.CancellationToken;
        using var user = await _factory.CreateAuthenticatedClientAsync(ct);
        var oldToken = await PairAsync(user, "ESKI-PC", ct);
        var oldAgent = _factory.CreateAgentConnection(oldToken);
        await using (oldAgent.ConfigureAwait(false))
        {
            var closedNotice = false;
            oldAgent.On(AgentHubContract.SessionClosed, () => closedNotice = true);
            await oldAgent.StartAsync(ct);
            await oldAgent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("ESKI-PC", "0.5.0"), ct);

            var newAgent = _factory.CreateAgentConnection(await PairAsync(user, "YENI-PC", ct));
            await using (newAgent.ConfigureAwait(false))
            {
                await Eventually.WithinAsync(() => Task.FromResult(closedNotice && oldAgent.State == HubConnectionState.Disconnected), Limit, ct);
                await newAgent.StartAsync(ct);
                await newAgent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("YENI-PC", "0.5.0"), ct);

                var status = await StatusAsync(user, ct);
                status.GetProperty("agent").GetProperty("machineName").GetString().ShouldBe("YENI-PC");
            }

            var again = _factory.CreateAgentConnection(oldToken);
            await using (again.ConfigureAwait(false))
            {
                (await Should.ThrowAsync<HttpRequestException>(() => again.StartAsync(ct))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            }
        }
    }

    /// <summary>
    /// Ajanı bağlı kullanıcı web'den çıkış yapınca ajan en geç 10 sn'de "oturum kapandı" bildirimini alır ve bağlantısı sunucu
    /// tarafından kapatılır; eski oturumuyla yeniden bağlanamaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Logout_ClosesAgentWithin10s_OldTokenRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        using var user = await _factory.CreateAuthenticatedClientAsync(ct);
        var token = await PairAsync(user, "CIKIS-PC", ct);
        var agent = _factory.CreateAgentConnection(token);
        await using (agent.ConfigureAwait(false))
        {
            var closedNotice = false;
            agent.On(AgentHubContract.SessionClosed, () => closedNotice = true);
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("CIKIS-PC", "0.5.0"), ct);

            using var logout = await user.PostAsync(new Uri("/api/auth/logout", UriKind.Relative), null, ct);

            logout.StatusCode.ShouldBe(HttpStatusCode.OK);
            await Eventually.WithinAsync(() => Task.FromResult(closedNotice && agent.State == HubConnectionState.Disconnected), Limit, ct);
        }

        var again = _factory.CreateAgentConnection(token);
        await using (again.ConfigureAwait(false))
        {
            (await Should.ThrowAsync<HttpRequestException>(() => again.StartAsync(ct))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    /// <summary>
    /// Eşleştirme, ajan bağlantısı ve çıkışta ajan düşürme boyunca üretilen log dosyası, teknik log ve olay kayıtları onay sırrını
    /// ve ajan oturum belirtecini içermez; eşleştirme olayı kodu da içermez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pairing_LogsAndEvents_NeverContainSecretOrToken()
    {
        var ct = TestContext.Current.CancellationToken;
        using var user = await _factory.CreateAuthenticatedClientAsync(ct);
        using var agentHttp = _factory.CreateClient();
        var (code, secret, _) = await AgentPairing.CreateCodeAsync(agentHttp, _factory.Services, ct, "SIR-PC");
        (await AgentPairing.DecideAsync(user, _factory.Services, "approve", code, ct)).Dispose();
        var (_, token) = await AgentPairing.StatusAsync(agentHttp, _factory.Services, secret, ct);
        token.ShouldNotBeNull();
        var agent = _factory.CreateAgentConnection(token);
        await using (agent.ConfigureAwait(false))
        {
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("SIR-PC", "0.5.0"), ct);
            (await user.PostAsync(new Uri("/api/auth/logout", UriKind.Relative), null, ct)).Dispose();
            await Eventually.WithinAsync(() => Task.FromResult(agent.State == HubConnectionState.Disconnected), Limit, ct);
        }

        var stale = _factory.CreateAgentConnection(token);
        await using (stale.ConfigureAwait(false))
        {
            await Should.ThrowAsync<HttpRequestException>(() => stale.StartAsync(ct));
        }

        var marker = Guid.NewGuid().ToString("N");
#pragma warning disable CA1848, CA2254 // Test: dosya logunun yazıldığını gösteren işaret.
        _factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Api.Tests.Flush").LogWarning($"flush {marker}");
#pragma warning restore CA1848, CA2254
        await Eventually.WithinAsync(() => Task.FromResult(_factory.ReadLogText().Contains(marker, StringComparison.Ordinal)), Limit, ct);
        var logText = _factory.ReadLogText();
        logText.ShouldContain("SIR-PC"); // akışın logları gerçekten dosyada
        var stored = string.Join('\n', _factory.Events.Events.Select(e => e.Message).Concat(_factory.TechnicalLogs.Logs.Select(l => l.Message + l.Exception)));

        foreach (var sensitive in new[] { secret, token, secret[4..], token[4..] })
        {
            logText.ShouldNotContain(sensitive);
            stored.ShouldNotContain(sensitive);
        }

        _factory.Events.Events.Where(e => e.Type == "identity.agent_paired").ShouldAllBe(e => !e.Message.Contains(code));
        logText.ShouldNotContain(code); // kod adreste taşınmaz, logda yok (review r1 M-2).
    }

    private static async Task<JsonElement> StatusAsync(HttpClient client, CancellationToken ct) =>
        await client.GetFromJsonAsync<JsonElement>("/api/system/status", ct);

    private async Task<string> PairAsync(HttpClient user, string machine, CancellationToken ct)
    {
        using var agentHttp = _factory.CreateClient();
        return await AgentPairing.PairAsync(user, agentHttp, _factory.Services, ct, machine);
    }
}
