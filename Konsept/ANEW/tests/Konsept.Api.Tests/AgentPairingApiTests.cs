using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Ajan eşleştirme uçları, hızlı profil (spec 0012 AC-1, AC-4, AC-6, AC-10, AC-11, AC-12 Api tarafı): kod alma, cihazı görme,
/// onay/ret, tek kullanımlık ve 10 dakikalık kod, tek teslim belirteç, çift onay, IP başına hız sınırı, olayın sahibi.
/// Saat <see cref="IdentityApiFactory.Time"/>; her test kendi istemcisini saati ilerlettikten sonra açar.
/// </summary>
public sealed class AgentPairingApiTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AgentPairingApiTests"/> class.</summary>
    /// <param name="factory">Kimlik test fabrikası.</param>
    public AgentPairingApiTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Ajan <c>ABCD-1234</c> biçiminde kod ve 10 dakikalık süre alır; kullanıcı kodla makine adı + sürümü görür ve onaylar;
    /// ajan belirtecini yalnızca bir kez alır; eşleştirme olayı kullanıcıya bağlı, cihaz adıyla yazılır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pairing_ApproveFlow_ShowsDevice_AgentGetsTokenOnce_EventOwned()
    {
        var ct = TestContext.Current.CancellationToken;
        var (user, userId) = await NewUserAsync(ct);
        using var agent = _factory.CreateClient();

        var (code, secret, expiresAt) = await AgentPairing.CreateCodeAsync(agent, _factory.Services, ct, "AKIN-PC", "0.5.0");
        code.ShouldMatch("^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$");
        expiresAt.ShouldBe(_factory.Time.GetUtcNow().AddMinutes(10));
        (await AgentPairing.StatusAsync(agent, _factory.Services, secret, ct)).State.ShouldBe("pending");

        using var lookup = await AgentPairing.DecideAsync(user, _factory.Services, "lookup", code.ToLowerInvariant().Replace("-", string.Empty, StringComparison.Ordinal), ct);
        var device = await lookup.Content.ReadFromJsonAsync<JsonElement>(ct);
        lookup.StatusCode.ShouldBe(HttpStatusCode.OK);
        device.GetProperty("machineName").GetString().ShouldBe("AKIN-PC");
        device.GetProperty("agentVersion").GetString().ShouldBe("0.5.0");

        using var approve = await AgentPairing.DecideAsync(user, _factory.Services, "approve", code, ct);
        approve.StatusCode.ShouldBe(HttpStatusCode.OK);

        var first = await AgentPairing.StatusAsync(agent, _factory.Services, secret, ct);
        first.State.ShouldBe("approved");
        first.Token.ShouldNotBeNullOrEmpty();
        var second = await AgentPairing.StatusAsync(agent, _factory.Services, secret, ct);
        second.State.ShouldBe("expired");
        second.Token.ShouldBeNull();

        _factory.AgentDevices.Devices.ShouldContain(d => d.UserId == userId && d.MachineName == "AKIN-PC" && d.RevokedAt == null);
        var paired = _factory.Events.Events.Single(e => e.Type == "identity.agent_paired" && e.Message.Contains("AKIN-PC", StringComparison.Ordinal));
        paired.UserId.ShouldBe(userId);
        paired.Message.ShouldBe("Ajan eşleştirildi: AKIN-PC (v0.5.0)");
        paired.Message.ShouldNotContain(code);
    }

    /// <summary>"Reddet" sonrası ajan reddedildiğini görür ve belirteç almaz; kod artık onaylanamaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pairing_Reject_AgentSeesRejected_NoToken()
    {
        var ct = TestContext.Current.CancellationToken;
        var (user, _) = await NewUserAsync(ct);
        using var agent = _factory.CreateClient();
        var (code, secret, _) = await AgentPairing.CreateCodeAsync(agent, _factory.Services, ct);

        using var reject = await AgentPairing.DecideAsync(user, _factory.Services, "reject", code, ct);
        using var approve = await AgentPairing.DecideAsync(user, _factory.Services, "approve", code, ct);
        var status = await AgentPairing.StatusAsync(agent, _factory.Services, secret, ct);

        reject.StatusCode.ShouldBe(HttpStatusCode.OK);
        approve.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        status.State.ShouldBe("rejected");
        status.Token.ShouldBeNull();
    }

    /// <summary>Var olmayan, kullanılmış ya da süresi dolmuş (10 dk) kod onaylanamaz ve Türkçe hata verir; ajan "süresi doldu" görür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pairing_ExpiredUsedUnknownCode_TurkishErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        using var agent = _factory.CreateClient();
        var (expiredCode, expiredSecret, _) = await AgentPairing.CreateCodeAsync(agent, _factory.Services, ct);
        _factory.Time.Advance(TimeSpan.FromMinutes(10));
        var (user, _) = await NewUserAsync(ct);
        var (usedCode, _, _) = await AgentPairing.CreateCodeAsync(agent, _factory.Services, ct);
        (await AgentPairing.DecideAsync(user, _factory.Services, "approve", usedCode, ct)).Dispose();

        var unknown = await ProblemAsync(await AgentPairing.DecideAsync(user, _factory.Services, "approve", "ZZZZ-9999", ct), ct);
        var used = await ProblemAsync(await AgentPairing.DecideAsync(user, _factory.Services, "approve", usedCode, ct), ct);
        var expired = await ProblemAsync(await AgentPairing.DecideAsync(user, _factory.Services, "approve", expiredCode, ct), ct);

        unknown.Status.ShouldBe(HttpStatusCode.NotFound);
        unknown.Title.ShouldBe("Kod bulunamadı. Ajan penceresindeki kodu kontrol edin.");
        used.Status.ShouldBe(HttpStatusCode.Conflict);
        used.Title.ShouldBe("Bu kod daha önce kullanılmış.");
        expired.Status.ShouldBe(HttpStatusCode.Conflict);
        expired.Title.ShouldBe("Kodun süresi dolmuş. Ajanda yeni kod alın.");
        (await AgentPairing.StatusAsync(agent, _factory.Services, expiredSecret, ct)).State.ShouldBe("expired");
    }

    /// <summary>Aynı kod aynı anda iki kez onaylanırsa yalnızca biri başarılı olur; tek cihaz ve tek eşleştirme olayı yazılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pairing_DoubleApprove_OnlyOneSucceeds_OneEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        var (user, userId) = await NewUserAsync(ct);
        using var agent = _factory.CreateClient();
        var machine = "CIFT-" + Guid.NewGuid().ToString("N")[..6];
        var (code, _, _) = await AgentPairing.CreateCodeAsync(agent, _factory.Services, ct, machine);

        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => AgentPairing.DecideAsync(user, _factory.Services, "approve", code, ct)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        _factory.AgentDevices.Devices.Count(d => d.UserId == userId).ShouldBe(1);
        _factory.Events.Events.Count(e => e.Type == "identity.agent_paired" && e.Message.Contains(machine, StringComparison.Ordinal)).ShouldBe(1);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Aynı IP'den bir dakikada 11. kod isteği (alma ya da onay) 429 ve "Çok fazla deneme" alır; 1 dakika sonra yeniden mümkündür.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pairing_RateLimit_11thRejected_ReopensAfterMinute()
    {
        var ct = TestContext.Current.CancellationToken;
        var (user, _) = await NewUserAsync(ct);
        using var agent = _factory.CreateClient();
        var ip = AuthenticatedClient.NewIp();
        var body = new { machineName = "PC", agentVersion = "0.5.0" };
        for (var i = 0; i < 9; i++)
        {
            using var ok = await AgentPairing.SendAsync(agent, _factory.Services, HttpMethod.Post, "/api/agent-pairing/codes", body, ct, ip);
            ok.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var tenth = await AgentPairing.DecideAsync(user, _factory.Services, "approve", "ZZZZ-9999", ct, ip);
        using var eleventh = await AgentPairing.DecideAsync(user, _factory.Services, "approve", "ZZZZ-9999", ct, ip);
        using var twelfth = await AgentPairing.SendAsync(agent, _factory.Services, HttpMethod.Post, "/api/agent-pairing/codes", body, ct, ip);
        var problem = await eleventh.Content.ReadFromJsonAsync<JsonElement>(ct);

        tenth.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        eleventh.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        problem.GetProperty("title").GetString().ShouldBe("Çok fazla deneme, biraz sonra tekrar deneyin");
        twelfth.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        _factory.Time.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(1)));
        using var later = await AgentPairing.SendAsync(agent, _factory.Services, HttpMethod.Post, "/api/agent-pairing/codes", body, ct, ip);
        later.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>B, A'nın ajanının ekrandaki kodunu kendi hesabıyla onaylarsa ajan B'ye bağlanır, A'ya değil.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pairing_OtherUserApprovesCode_AgentBelongsToApprover()
    {
        var ct = TestContext.Current.CancellationToken;
        var (_, userA) = await NewUserAsync(ct);
        var (clientB, userB) = await NewUserAsync(ct);
        using var agent = _factory.CreateClient();
        var machine = "A-PC-" + Guid.NewGuid().ToString("N")[..6];
        var (code, _, _) = await AgentPairing.CreateCodeAsync(agent, _factory.Services, ct, machine);

        using var approve = await AgentPairing.DecideAsync(clientB, _factory.Services, "approve", code, ct);

        approve.StatusCode.ShouldBe(HttpStatusCode.OK);
        var device = _factory.AgentDevices.Devices.Single(d => d.MachineName == machine);
        device.UserId.ShouldBe(userB);
        device.UserId.ShouldNotBe(userA);
    }

    /// <summary>
    /// Ajan oturumu son kullanımdan itibaren 90 gün geçerlidir: 90 gün dolmadan bağlanan ajanın oturumu yenilenir (yeniden
    /// eşleştirme istenmez), 90 günden uzun kullanılmayan oturum reddedilir (saat ilerletilerek).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentSession_90DaysUnused_Rejected_UsedWithinRenews()
    {
        var ct = TestContext.Current.CancellationToken;
        var (user, _) = await NewUserAsync(ct);
        using var agent = _factory.CreateClient();
        var token = await AgentPairing.PairAsync(user, agent, _factory.Services, ct, "UZUN-PC");

        (await NegotiateAsync(agent, token, ct)).ShouldBe(HttpStatusCode.OK);
        _factory.Time.Advance(TimeSpan.FromDays(89));
        (await NegotiateAsync(agent, token, ct)).ShouldBe(HttpStatusCode.OK);
        _factory.Time.Advance(TimeSpan.FromDays(89));
        (await NegotiateAsync(agent, token, ct)).ShouldBe(HttpStatusCode.OK);
        _factory.Time.Advance(TimeSpan.FromDays(90).Add(TimeSpan.FromMinutes(1)));
        (await NegotiateAsync(agent, token, ct)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<HttpStatusCode> NegotiateAsync(HttpClient client, string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/agent/negotiate?negotiateVersion=1");
        request.Headers.Authorization = new("Bearer", token);
        using var response = await client.SendAsync(request, ct);
        return response.StatusCode;
    }

    private static async Task<(HttpStatusCode Status, string? Title)> ProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        using (response)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            return (response.StatusCode, body.GetProperty("title").GetString());
        }
    }

    private async Task<(HttpClient Client, Guid UserId)> NewUserAsync(CancellationToken ct)
    {
        var email = AuthenticatedClient.NewEmail();
        var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        return (client, _factory.Users.Get(email).Id);
    }
}
