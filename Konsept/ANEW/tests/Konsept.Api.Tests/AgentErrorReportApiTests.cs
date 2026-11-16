using System.Net.Http;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.Monitoring.Domain.Enums;
using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Ajan hata bildiriminin API tarafı (spec 0022 AC-5, AC-8, AC-9, AC-16): kimlikli hub üzerinden kaynak "ajan" kaydı (makine adı yok,
/// bileşen/adım saklı), kimliksiz bağlantı reddi, başka hesapta oluşan bildirimin reddi, eşleşme öncesi bildirimin sahibe gitmesi.
/// </summary>
public sealed class AgentErrorReportApiTests : IClassFixture<FastApiFactory>
{
    private const string Machine = "GIZLI-MAKINE-0022";
    private readonly FastApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AgentErrorReportApiTests"/> class.</summary>
    /// <param name="factory">Hızlı API fabrikası.</param>
    public AgentErrorReportApiTests(FastApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Eşleşmiş ajanın bildirimi kaynak "ajan", "Yeni", ajan versiyonu, işletim sistemi, özet, bileşen, adım ve log satırlarıyla saklanır;
    /// kayıtta makine adı geçmez; hesabı <c>WhoAmI</c> sahibin kimliğidir; eşleşme öncesi oluşan (hesapsız) bildirim de sahibe gider;
    /// Günlük'e "Hata bildirimi gönderildi" düşer.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentHub_ReportError_StoredAsAgentWithoutMachineName()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var user = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var userId = _factory.Users.Get(email).Id;
        var agent = _factory.CreateAgentConnection(await PairAsync(user, ct));
        await using (agent.ConfigureAwait(false))
        {
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration(Machine, "0.9.0"), ct);
            var account = await agent.InvokeAsync<Guid>(AgentHubContract.WhoAmI, ct);
            var reply = await agent.InvokeAsync<AgentErrorReportReply>(
                AgentHubContract.ReportError,
                new AgentErrorReportRequest(account, "FFmpeg indirilemedi", "0.9.0", "Windows 11", "FFmpeg", "indirme", ["12:00:00Z [WRN] yeniden deneniyor"]),
                ct);
            var beforePairing = await agent.InvokeAsync<AgentErrorReportReply>(
                AgentHubContract.ReportError,
                new AgentErrorReportRequest(null, "eşleşme öncesi hata", "0.9.0", "Windows 11", null, null, []),
                ct);

            account.ShouldBe(userId);
            reply.Result.ShouldBe(AgentErrorReportResult.Sent);
            beforePairing.Result.ShouldBe(AgentErrorReportResult.Sent);
            var report = _factory.ErrorReports.Reports.Single(r => r.Number == reply.Number);
            report.Source.ShouldBe(ErrorReportSource.Agent);
            report.Status.ShouldBe(ErrorReportStatus.New);
            report.UserId.ShouldBe(userId);
            report.AgentVersion.ShouldBe("0.9.0");
            report.OperatingSystem.ShouldBe("Windows 11");
            report.Description.ShouldBe("FFmpeg indirilemedi");
            report.Component.ShouldBe("FFmpeg");
            report.Step.ShouldBe("indirme");
            report.Attachments.ShouldBe("12:00:00Z [WRN] yeniden deneniyor");
            _factory.ErrorReports.Reports.Single(r => r.Number == beforePairing.Number).UserId.ShouldBe(userId);
            _factory.ErrorReports.Reports.Where(r => r.UserId == userId)
                .ShouldAllBe(r => !string.Concat(r.Description, r.Attachments, r.Component, r.OperatingSystem).Contains(Machine));
            _factory.Events.Events.Count(e => e.UserId == userId && e.Type == "error_report.sent").ShouldBe(2);
        }
    }

    /// <summary>
    /// Ajanın gönderebileceği en büyük rapor (200 satır × 500 karakter, Türkçe karakterli; özet 4000) hub sınırına takılmadan kaydedilir;
    /// bağlantı kopmaz (review r1 H-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentHub_ReportError_LargestReportAccepted()
    {
        var ct = TestContext.Current.CancellationToken;
        using var user = await AuthenticatedClient.CreateAsync(_factory, ct);
        var agent = _factory.CreateAgentConnection(await PairAsync(user, ct));
        await using (agent.ConfigureAwait(false))
        {
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration(Machine, "0.9.0"), ct);
            var lines = Enumerable.Range(0, 200).Select(i => $"{i:000} " + new string('ğ', 496)).ToList();

            var reply = await agent.InvokeAsync<AgentErrorReportReply>(
                AgentHubContract.ReportError,
                new AgentErrorReportRequest(null, new string('ş', 4000), "0.9.0", "Windows 11", null, null, lines),
                ct);

            reply.Result.ShouldBe(AgentErrorReportResult.Sent);
            agent.State.ShouldBe(HubConnectionState.Connected);
        }
    }

    /// <summary>A hesabında oluşan bildirim B hesabıyla eşleşmiş ajandan gelirse kaydedilmez ("başka hesap").</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentHub_ReportError_OtherAccountNotStored()
    {
        var ct = TestContext.Current.CancellationToken;
        var emailA = AuthenticatedClient.NewEmail();
        using var userA = await AuthenticatedClient.CreateAsync(_factory, ct, emailA);
        using var userB = await AuthenticatedClient.CreateAsync(_factory, ct);
        var idA = _factory.Users.Get(emailA).Id;
        var agent = _factory.CreateAgentConnection(await PairAsync(userB, ct));
        await using (agent.ConfigureAwait(false))
        {
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration(Machine, "0.9.0"), ct);
            var before = _factory.ErrorReports.Reports.Count;

            var reply = await agent.InvokeAsync<AgentErrorReportReply>(
                AgentHubContract.ReportError,
                new AgentErrorReportRequest(idA, "A hesabında oluşan hata", "0.9.0", "Windows 11", null, null, []),
                ct);

            reply.Result.ShouldBe(AgentErrorReportResult.OtherAccount);
            reply.Number.ShouldBeNull();
            _factory.ErrorReports.Reports.Count.ShouldBe(before);
        }
    }

    /// <summary>Ajan kimliği olmadan (belirteçsiz ya da sahte belirteçle) hub'a bağlanılamaz; kimliksiz bildirim yolu yoktur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentHub_ReportError_WithoutTokenRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        foreach (var token in new string?[] { null, "sahte-belirtec" })
        {
            var agent = _factory.CreateAgentConnection(token);
            await using (agent.ConfigureAwait(false))
            {
                await Should.ThrowAsync<HttpRequestException>(() => agent.StartAsync(ct));
            }
        }

        using var anonymous = _factory.CreateClient();
        using var http = await anonymous.PostAsync(new Uri("/api/agent/error-reports", UriKind.Relative), null, ct);
        ((int)http.StatusCode).ShouldBeOneOf(401, 404, 405);
    }

    private async Task<string> PairAsync(HttpClient user, CancellationToken ct)
    {
        using var agentHttp = _factory.CreateClient();
        return await AgentPairing.PairAsync(user, agentHttp, _factory.Services, ct, Machine);
    }
}
