using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.Concepts.Application.Concrete;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Identity.Application;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Production.Domain.Entities;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Admin Kullanıcılar ve ban, Api tarafı (spec 0024 AC-1, AC-2, AC-4…AC-13, AC-15). Bellek depoları; gerçek sorgular
/// <c>AdminUsersIntegrationTests</c>'te. Saat 2026-11-14 09:00 UTC sabittir.
/// </summary>
public sealed class AdminUsersApiTests
{
    private const string Users = "/api/admin/users";
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    /// <summary>
    /// <c>admin.panel</c> yetkisi olmayan kullanıcının token'ıyla liste, ban ve ban kaldırma 403 döner, veri dönmez, hedef banlanmaz
    /// ve kayıt oluşmaz; kimliksiz istek 401. (Kasıtlı bozma: politika kaldırılınca kırmızı — plan Build evidence.)
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminUsers_UserToken_403NoDataNoChange_Anonymous401()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var user = await AuthenticatedClient.CreateAsync(factory, ct);
        using var anonymous = factory.CreateClient();
        var target = await AddUserAsync(factory, "Hedef Kişi", "hedef@ornek.test", ct);
        var banned = await AddUserAsync(factory, "Banlı Kişi", "banli@ornek.test", ct);
        factory.Users.Users.Single(u => u.Id == banned).Ban(factory.Time.GetUtcNow()); // yetkisiz unban'ın değiştirmediği görülsün (review r1 I-3).

        foreach (var client in new[] { user, anonymous })
        {
            using var list = await client.GetAsync(new Uri(Users + "?search=he", UriKind.Relative), ct);
            using var ban = await client.PostAsJsonAsync($"{Users}/{target}/ban", new { reason = "x" }, ct);
            using var unban = await client.PostAsJsonAsync($"{Users}/{banned}/unban", new { }, ct);
            var expected = client == user ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;

            list.StatusCode.ShouldBe(expected);
            ban.StatusCode.ShouldBe(expected);
            unban.StatusCode.ShouldBe(expected);
            (await list.Content.ReadAsStringAsync(ct)).ShouldNotContain("hedef@ornek.test");
        }

        factory.Users.Users.Single(u => u.Id == target).IsBanned.ShouldBeFalse();
        factory.Users.Users.Single(u => u.Id == banned).IsBanned.ShouldBeTrue();
        factory.Bans.Records.ShouldBeEmpty();
    }

    /// <summary>
    /// Yarıda kalan ban (durum yazıldı, kayıt yazılamadı → 500) yeniden denenince tamamlanır: web oturumları ve ajan cihazı iptal edilir,
    /// eski token reddedilir (review r1 M-1; AC-5, AC-6, AC-10).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ban_FailedHalfway_RetryStillRevokesSessionsAndAgent()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var (user, _, id) = await SignedInUserAsync(factory, ct);
        using var agentHttp = factory.CreateClient();
        await AgentPairing.PairAsync(user, agentHttp, factory.Services, ct, "YARIM-PC");
        factory.Bans.FailNext = new InvalidOperationException("kayıt yazılamadı");

        using var first = await admin.PostAsJsonAsync($"{Users}/{id}/ban", new { }, ct);
        using var retry = await admin.PostAsJsonAsync($"{Users}/{id}/ban", new { }, ct);
        using var session = await user.GetAsync(new Uri("/api/auth/session", UriKind.Relative), ct);

        ((int)first.StatusCode).ShouldBe(500);
        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Sessions.Sessions.Where(s => s.UserId == id).ShouldAllBe(s => s.RevokedAt != null);
        factory.AgentDevices.Devices.Where(d => d.UserId == id).ShouldAllBe(d => d.RevokedAt != null);
        session.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        user.Dispose();
    }

    /// <summary>
    /// Bilinen veri setinde sütunlar (ad, e-posta, kayıt, son giriş, durum, rol, klip — yeni sürüm ayrı klip değil), 25'erli sayfa,
    /// büyük/küçük harf duyarsız "içerir" araması (2 karakterden kısası uygulanmaz), Tümü/Aktif/Banlı filtresi ve eşleşme yokken boş
    /// sayfa; yanıtlarda konsept/video başlığı, profil ya da key yoktur (AC-2, AC-13).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task List_KnownDataset_ColumnsPagingSearchStatus_NoContent()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var ayse = await AddUserAsync(factory, "Ayşe Yılmaz", "ayse@ornek.test", ct);
        var veli = await AddUserAsync(factory, "Veli Demir", "VELI@ornek.test", ct);
        for (var i = 0; i < 28; i++)
        {
            await AddUserAsync(factory, $"Doldurma {i:00}", $"dolgu{i:00}@ornek.test", ct);
        }

        var concept = Guid.NewGuid();
        await AddVideoAsync(factory, ayse, concept, ct);
        await AddVideoAsync(factory, ayse, concept, ct); // yeni sürüm: ayrı klip değil.
        await AddVideoAsync(factory, ayse, Guid.NewGuid(), ct);
        (await admin.PostAsJsonAsync($"{Users}/{veli}/ban", new { reason = "GIZLISEBEP" }, ct)).EnsureSuccessStatusCode();

        var first = await admin.GetFromJsonAsync<JsonElement>(Users, ct);
        var second = await admin.GetFromJsonAsync<JsonElement>(Users + "?page=2", ct);
        var search = await admin.GetFromJsonAsync<JsonElement>(Users + "?search=AYŞE", ct);
        var searchAscii = await admin.GetFromJsonAsync<JsonElement>(Users + "?search=veli", ct);
        var tooShort = await admin.GetFromJsonAsync<JsonElement>(Users + "?search=a", ct);
        var banned = await admin.GetFromJsonAsync<JsonElement>(Users + "?status=banned", ct);
        var active = await admin.GetFromJsonAsync<JsonElement>(Users + "?status=active", ct);
        var none = await admin.GetFromJsonAsync<JsonElement>(Users + "?search=yokboyle", ct);
        var raw = await admin.GetStringAsync(new Uri(Users + "?search=ay", UriKind.Relative), ct);

        first.GetProperty("total").GetInt32().ShouldBe(31);
        first.GetProperty("pageSize").GetInt32().ShouldBe(25);
        first.GetProperty("items").GetArrayLength().ShouldBe(25);
        second.GetProperty("items").GetArrayLength().ShouldBe(6);
        var row = search.GetProperty("items").EnumerateArray().Single();
        row.GetProperty("name").GetString().ShouldBe("Ayşe Yılmaz");
        row.GetProperty("email").GetString().ShouldBe("ayse@ornek.test");
        row.GetProperty("registeredAt").GetDateTimeOffset().ShouldBe(factory.Time.GetUtcNow());
        row.GetProperty("lastLoginAt").ValueKind.ShouldBe(JsonValueKind.Null);
        row.GetProperty("status").GetString().ShouldBe("active");
        row.GetProperty("role").GetString().ShouldBe("user");
        row.GetProperty("clips").GetInt32().ShouldBe(2);
        search.GetProperty("filtered").GetBoolean().ShouldBeTrue();
        searchAscii.GetProperty("items").EnumerateArray().Single().GetProperty("status").GetString().ShouldBe("banned");
        tooShort.GetProperty("total").GetInt32().ShouldBe(31);
        tooShort.GetProperty("filtered").GetBoolean().ShouldBeFalse();
        banned.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("email").GetString()).ShouldBe(["VELI@ornek.test"]);
        active.GetProperty("total").GetInt32().ShouldBe(30);
        none.GetProperty("items").GetArrayLength().ShouldBe(0);
        none.GetProperty("filtered").GetBoolean().ShouldBeTrue();
        var adminRow = (await admin.GetFromJsonAsync<JsonElement>(Users + "?search=admin@ornek", ct)).GetProperty("items").EnumerateArray().Single();
        adminRow.GetProperty("role").GetString().ShouldBe("admin");
        adminRow.GetProperty("isSelf").GetBoolean().ShouldBeTrue();
        adminRow.GetProperty("lastLoginAt").GetDateTimeOffset().ShouldBe(factory.Time.GetUtcNow());
        foreach (var secret in new[] { "GIZLIVIDEOBASLIK", "GIZLIKONSEPT", "sk-", "passwordHash", "hash" })
        {
            raw.ShouldNotContain(secret);
        }
    }

    /// <summary>
    /// Ban: satır "banned" olur, kayıt yapan admin, an ve sebeple görünür; tekrar ve eşzamanlı ban ikinci kayıt oluşturmaz; var
    /// olmayan kullanıcı 404; 500 karakteri aşan sebep 400 ve ban yok; boş sebep geçerlidir (AC-4).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ban_RecordShown_Idempotent_UnknownAndLongReasonRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var target = await AddUserAsync(factory, "Hedef", "hedef@ornek.test", ct);
        var other = await AddUserAsync(factory, "Diğer", "diger@ornek.test", ct);

        using var tooLong = await admin.PostAsJsonAsync($"{Users}/{target}/ban", new { reason = new string('a', 501) }, ct);
        tooLong.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Users.Users.Single(u => u.Id == target).IsBanned.ShouldBeFalse();

        var responses = await Task.WhenAll(
            admin.PostAsJsonAsync($"{Users}/{target}/ban", new { reason = new string('b', 500) }, ct),
            admin.PostAsJsonAsync($"{Users}/{target}/ban", new { reason = new string('b', 500) }, ct));
        using var again = await admin.PostAsJsonAsync($"{Users}/{target}/ban", new { reason = "ikinci" }, ct);
        using var unknown = await admin.PostAsJsonAsync($"{Users}/{Guid.NewGuid()}/ban", new { }, ct);
        using var noReason = await admin.PostAsJsonAsync($"{Users}/{other}/ban", new { }, ct);

        foreach (var response in responses)
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Dispose();
        }

        var row = await again.Content.ReadFromJsonAsync<JsonElement>(ct);
        again.StatusCode.ShouldBe(HttpStatusCode.OK);
        row.GetProperty("status").GetString().ShouldBe("banned");
        var history = row.GetProperty("history").EnumerateArray().ToList();
        history.Count.ShouldBe(1);
        history[0].GetProperty("action").GetString().ShouldBe("ban");
        history[0].GetProperty("adminName").GetString().ShouldBe("Admin");
        history[0].GetProperty("at").GetDateTimeOffset().ShouldBe(factory.Time.GetUtcNow());
        history[0].GetProperty("reason").GetString().ShouldBe(new string('b', 500));
        factory.Bans.Records.Count(r => r.UserId == target).ShouldBe(1);
        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        noReason.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Bans.Records.Single(r => r.UserId == other).Reason.ShouldBeNull();
    }

    /// <summary>
    /// Banlanan kullanıcının açık oturumu bir sonraki istekte geçersizdir: ban öncesi alınmış, süresi dolmamış erişim token'ı ve
    /// yenileme token'ı reddedilir (AC-5); web'den hata bildirimi reddedilir ve kayıt oluşmaz (AC-9).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ban_OpenSessionRejected_AccessRefreshAndErrorReport()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var (user, session, id) = await SignedInUserAsync(factory, ct);
        (await user.GetAsync(new Uri("/api/auth/session", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await admin.PostAsJsonAsync($"{Users}/{id}/ban", new { }, ct)).EnsureSuccessStatusCode();

        using var current = await user.GetAsync(new Uri("/api/auth/session", UriKind.Relative), ct);
        using var refresh = await AuthenticatedClient.PostAsync(
            user, factory.Services, "/api/auth/refresh", new { refreshToken = session.GetProperty("refreshToken").GetString() }, ct);
        using var report = await user.PostAsJsonAsync("/api/error-reports", new { description = "banlıyım", page = "/" }, ct);

        current.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        report.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.ErrorReports.Reports.ShouldBeEmpty();
        factory.Sessions.Sessions.Where(s => s.UserId == id).ShouldAllBe(s => s.RevokedAt != null);
        user.Dispose();
    }

    /// <summary>
    /// Banlanan kullanıcının bağlı ajanı en geç 10 sn'de "oturum kapandı" bildirimini alır ve bağlantısı kapanır; aynı belirteçle
    /// yeniden bağlanamaz (bekletilmiş bildirimler de gönderilemez); yeni cihaz eşleştirmesi reddedilir (AC-6, AC-9).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ban_ClosesAgentWithin10s_ReconnectAndPairingRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var (user, _, id) = await SignedInUserAsync(factory, ct);
        using var agentHttp = factory.CreateClient();
        var token = await AgentPairing.PairAsync(user, agentHttp, factory.Services, ct, "BAN-PC");
        var agent = AgentConnection(factory, token);
        await using (agent.ConfigureAwait(false))
        {
            var closedNotice = false;
            agent.On(AgentHubContract.SessionClosed, () => closedNotice = true);
            await agent.StartAsync(ct);
            await agent.InvokeAsync(AgentHubContract.Register, new AgentRegistration("BAN-PC", "0.5.0"), ct);

            (await admin.PostAsJsonAsync($"{Users}/{id}/ban", new { }, ct)).EnsureSuccessStatusCode();

            await Eventually.WithinAsync(() => Task.FromResult(closedNotice && agent.State == HubConnectionState.Disconnected), Limit, ct);
        }

        var again = AgentConnection(factory, token);
        await using (again.ConfigureAwait(false))
        {
            (await Should.ThrowAsync<HttpRequestException>(() => again.StartAsync(ct))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var (code, _, _) = await AgentPairing.CreateCodeAsync(agentHttp, factory.Services, ct, "YENI-PC");
        using var approve = await AgentPairing.DecideAsync(user, factory.Services, "approve", code, ct);
        approve.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.AgentDevices.Devices.Where(d => d.UserId == id).ShouldAllBe(d => d.RevokedAt != null);
        user.Dispose();
    }

    /// <summary>
    /// Ban oturum sonu dinleyicilerini (0006 Req 16 üretim durdurma, 0014 Req 20 konsept partisi) banlanan kullanıcı için çağırır;
    /// kullanıcının videoları silinmez (AC-7). Üretim dinleyicisinin işi Durduruldu yaptığı 0006'nın çıkış testinde
    /// (<c>ProductionApiTests.Logout_StopsRunningJob</c>) kanıtlıdır; aynı dinleyici burada çağrılır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ban_StopsBackgroundWork_ViaSessionEndListeners_NoDataDeleted()
    {
        var ct = TestContext.Current.CancellationToken;
        var recorder = new RecordingSessionsEndedListener();
        await using var baseFactory = new IdentityApiFactory();
        await using var factory = baseFactory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IUserSessionsEndedListener>(recorder)));
        using var admin = await baseFactory.CreateAdminClientAsync(ct);
        var target = await AddUserAsync(baseFactory, "Üretici", "uretici@ornek.test", ct);
        await AddVideoAsync(baseFactory, target, Guid.NewGuid(), ct);
        using var factoryAdmin = factory.CreateClient();
        factoryAdmin.DefaultRequestHeaders.Authorization = admin.DefaultRequestHeaders.Authorization;

        (await factoryAdmin.PostAsJsonAsync($"{Users}/{target}/ban", new { }, ct)).EnsureSuccessStatusCode();
        (await factoryAdmin.PostAsJsonAsync($"{Users}/{target}/ban", new { }, ct)).EnsureSuccessStatusCode();

        recorder.Users.ShouldBe([target, target]); // plan 0024 A-3: tekrar ban da iptali (idempotent) yeniden çalıştırır (review r1 M-1).
        baseFactory.Bans.Records.Count(r => r.UserId == target).ShouldBe(1); // kayıt yine tek.
        baseFactory.ProducedVideos.Rows.Count(v => v.UserId == target).ShouldBe(1);
        baseFactory.Users.Users.ShouldContain(u => u.Id == target);
    }

    /// <summary>
    /// Oturum sonu (çıkış ya da ban) konsept dinleyicisi kullanıcının iki şeritteki süren partisine durdurma uygular (başlamamış
    /// konseptler çağrılmaz, 0014 Req 19); başka kullanıcının işi sürer (AC-7; B-92).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptListener_StopsUsersRunningBatchesOnly()
    {
        var registry = new ConceptJobRegistry();
        var user = Guid.NewGuid();
        var topic = new ConceptJob { UserId = user, ProfileName = "p", Lane = JobLane.Topic, Total = 20 };
        var idea = new ConceptJob { UserId = user, ProfileName = "p", Lane = JobLane.Idea, Total = 5 };
        var stranger = new ConceptJob { UserId = Guid.NewGuid(), ProfileName = "p", Lane = JobLane.Topic, Total = 5 };
        registry.TryStart(topic).ShouldBeTrue();
        registry.TryStart(idea).ShouldBeTrue();
        registry.TryStart(stranger).ShouldBeTrue();

        await new ConceptSessionListener(registry).OnAllSessionsEndedAsync(user, TestContext.Current.CancellationToken);

        topic.StopRequested.ShouldBeTrue();
        idea.StopRequested.ShouldBeTrue();
        stranger.StopRequested.ShouldBeFalse();
    }

    /// <summary>
    /// Banlı hesap: doğru şifre "Hesabınız askıya alınmıştır." (sebep yok), sayaç değişmez (AC-8). Yanlış şifre 0002 akışı: 2 hak, 1 hak,
    /// 3.'de 15 dk kilit ve açılma zamanı; hiçbirinde "askıya" yok; kilitliyken doğru şifre kilit mesajı; kilit dolunca doğru şifre
    /// askı mesajı, giriş yok, sayaç değişmez (AC-15).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_Banned_SuspendedOnCorrect_NormalLockoutOnWrong()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var (user, _, id) = await SignedInUserAsync(factory, ct, "askida@ornek.test");
        user.Dispose();
        (await admin.PostAsJsonAsync($"{Users}/{id}/ban", new { reason = "GIZLISEBEP" }, ct)).EnsureSuccessStatusCode();
        using var client = factory.CreateClient();

        var correct = await LoginAsync(factory, client, "askida@ornek.test", AuthenticatedClient.Password, ct);
        var countAfterCorrect = factory.Users.Get("askida@ornek.test").FailedLoginCount;
        var first = await LoginAsync(factory, client, "askida@ornek.test", "Yanlis123", ct);
        var second = await LoginAsync(factory, client, "askida@ornek.test", "Yanlis123", ct);
        var third = await LoginAsync(factory, client, "askida@ornek.test", "Yanlis123", ct);
        var lockedCorrect = await LoginAsync(factory, client, "askida@ornek.test", AuthenticatedClient.Password, ct);
        factory.Time.Advance(TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(1)));
        var before = factory.Users.Get("askida@ornek.test").FailedLoginCount;
        var afterLock = await LoginAsync(factory, client, "askida@ornek.test", AuthenticatedClient.Password, ct);

        correct.GetProperty("code").GetString().ShouldBe(IdentityErrors.SuspendedCode);
        correct.GetProperty("title").GetString().ShouldBe("Hesabınız askıya alınmıştır.");
        correct.ToString().ShouldNotContain("GIZLISEBEP");
        countAfterCorrect.ShouldBe(0);
        first.GetProperty("title").GetString().ShouldBe("Yanlış girdiniz, 2 hakkınız kaldı");
        second.GetProperty("title").GetString().ShouldBe("Yanlış girdiniz, 1 hakkınız kaldı");
        third.GetProperty("code").GetString().ShouldBe(IdentityErrors.LockedCode);
        third.GetProperty("lockedUntil").GetDateTimeOffset().ShouldBe(factory.Time.GetUtcNow().AddSeconds(-1));
        foreach (var wrong in new[] { first, second, third, lockedCorrect })
        {
            wrong.ToString().ShouldNotContain("askıya");
        }

        lockedCorrect.GetProperty("code").GetString().ShouldBe(IdentityErrors.LockedCode);
        afterLock.GetProperty("code").GetString().ShouldBe(IdentityErrors.SuspendedCode);
        factory.Users.Get("askida@ornek.test").FailedLoginCount.ShouldBe(before);
        factory.Sessions.Sessions.Where(s => s.UserId == id).ShouldAllBe(s => s.RevokedAt != null);
    }

    /// <summary>
    /// Ban kaldırma: kullanıcı yeniden giriş yapabilir, ban öncesi token'lar geçersiz kalır, kayıt (yapan admin, an) görünür; ikinci
    /// kaldırma kayıt üretmez (AC-10).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Unban_CanLoginAgain_OldTokensStayInvalid_RecordShown()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var (user, _, id) = await SignedInUserAsync(factory, ct, "geri@ornek.test");
        (await admin.PostAsJsonAsync($"{Users}/{id}/ban", new { }, ct)).EnsureSuccessStatusCode();
        factory.Time.Advance(TimeSpan.FromMinutes(1));

        using var unban = await admin.PostAsJsonAsync($"{Users}/{id}/unban", new { }, ct);
        (await admin.PostAsJsonAsync($"{Users}/{id}/unban", new { }, ct)).EnsureSuccessStatusCode();
        using var old = await user.GetAsync(new Uri("/api/auth/session", UriKind.Relative), ct);
        using var fresh = factory.CreateClient();
        var session = await AuthenticatedClient.LoginAsync(fresh, factory.Services, "geri@ornek.test", AuthenticatedClient.Password, ct);
        fresh.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        using var now = await fresh.GetAsync(new Uri("/api/auth/session", UriKind.Relative), ct);

        unban.StatusCode.ShouldBe(HttpStatusCode.OK);
        var row = await unban.Content.ReadFromJsonAsync<JsonElement>(ct);
        row.GetProperty("status").GetString().ShouldBe("active");
        var history = row.GetProperty("history").EnumerateArray().ToList();
        history.Select(h => h.GetProperty("action").GetString()).ShouldBe(["unban", "ban"]);
        history[0].GetProperty("adminName").GetString().ShouldBe("Admin");
        history[0].GetProperty("at").GetDateTimeOffset().ShouldBe(factory.Time.GetUtcNow());
        factory.Bans.Records.Count(r => r.UserId == id).ShouldBe(2);
        old.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        now.StatusCode.ShouldBe(HttpStatusCode.OK);
        user.Dispose();
    }

    /// <summary>
    /// Admin kendini ve başka bir admini banlayamaz: sunucu doğrudan istekte de reddeder ve hesap banlanmaz; listede kendi satırı
    /// <c>isSelf</c> ve iki satır da admin rolündedir (AC-11).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ban_AdminTargets_RejectedServerSide()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var self = factory.Users.Get(IdentityApiFactory.AdminEmail).Id;
        var otherAdmin = User.Register("Diğer Admin", "admin2@ornek.test", "hash", factory.Time.GetUtcNow(), Role.Admin);
        (await factory.Users.TryAddAsync(otherAdmin, ct)).ShouldBeTrue();

        using var banSelf = await admin.PostAsJsonAsync($"{Users}/{self}/ban", new { }, ct);
        using var banOther = await admin.PostAsJsonAsync($"{Users}/{otherAdmin.Id}/ban", new { }, ct);

        banSelf.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await banSelf.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("code").GetString().ShouldBe(IdentityErrors.CannotBanSelfCode);
        banOther.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await banOther.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("code").GetString().ShouldBe(IdentityErrors.CannotBanAdminCode);
        factory.Users.Users.ShouldAllBe(u => !u.IsBanned);
        factory.Bans.Records.ShouldBeEmpty();
        (await admin.GetAsync(new Uri("/api/auth/session", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// Aktiviteler'de "Banlandı" / "Ban kaldırıldı" (sebepsiz) görünür; İstatistikler'deki banlı kullanıcı sayısı 0 → 1 → 0 (AC-12).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task BanEvents_InActivities_BannedCountUpdates()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new IdentityApiFactory();
        using var admin = await factory.CreateAdminClientAsync(ct);
        var target = await AddUserAsync(factory, "Olay Kişi", "olay@ornek.test", ct);

        var none = await admin.GetFromJsonAsync<JsonElement>("/api/admin/statistics", ct);
        (await admin.PostAsJsonAsync($"{Users}/{target}/ban", new { reason = "GIZLISEBEP" }, ct)).EnsureSuccessStatusCode();
        var one = await admin.GetFromJsonAsync<JsonElement>("/api/admin/statistics", ct);
        (await admin.PostAsJsonAsync($"{Users}/{target}/unban", new { }, ct)).EnsureSuccessStatusCode();
        var zero = await admin.GetFromJsonAsync<JsonElement>("/api/admin/statistics", ct);
        var activities = await admin.GetStringAsync(new Uri("/api/admin/activities?user=olay", UriKind.Relative), ct);

        none.GetProperty("bannedUsers").GetInt32().ShouldBe(0);
        one.GetProperty("bannedUsers").GetInt32().ShouldBe(1);
        zero.GetProperty("bannedUsers").GetInt32().ShouldBe(0);
        activities.ShouldContain("Banlandı");
        activities.ShouldContain("Ban kaldırıldı");
        activities.ShouldNotContain("GIZLISEBEP");
        factory.Events.Events.Where(e => e.UserId == target).ShouldAllBe(e => !e.Message.Contains("GIZLISEBEP", StringComparison.Ordinal));
    }

    private static HubConnection AgentConnection(IdentityApiFactory factory, string token) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, AgentHubContract.HubPath.TrimStart('/')), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

    private static async Task<(HttpClient Client, JsonElement Session, Guid Id)> SignedInUserAsync(IdentityApiFactory factory, CancellationToken ct, string? email = null)
    {
        var address = email ?? AuthenticatedClient.NewEmail();
        var client = factory.CreateClient();
        await AuthenticatedClient.RegisterAsync(client, factory.Services, address, AuthenticatedClient.Password, "Kullanıcı", ct);
        var session = await AuthenticatedClient.LoginAsync(client, factory.Services, address, AuthenticatedClient.Password, ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        return (client, session, factory.Users.Get(address).Id);
    }

    private static async Task<JsonElement> LoginAsync(IdentityApiFactory factory, HttpClient client, string email, string password, CancellationToken ct)
    {
        using var response = await AuthenticatedClient.PostAsync(client, factory.Services, "/api/auth/login", new { email, password }, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    private static async Task<Guid> AddUserAsync(IdentityApiFactory factory, string name, string email, CancellationToken ct)
    {
        var user = User.Register(name, email, "hash", factory.Time.GetUtcNow());
        (await factory.Users.TryAddAsync(user, ct)).ShouldBeTrue();
        return user.Id;
    }

    private static Task AddVideoAsync(IdentityApiFactory factory, Guid userId, Guid conceptId, CancellationToken ct) =>
        factory.ProducedVideos.AddAsync(
            new ProducedVideo
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ProfileId = Guid.NewGuid(),
                ConceptId = conceptId,
                JobId = Guid.NewGuid(),
                Title = "GIZLIVIDEOBASLIK",
                ConceptText = "{\"t\":\"GIZLIKONSEPT\"}",
                Engine = "remotion",
                FileName = "v.mp4",
                StartedAt = factory.Time.GetUtcNow().AddMinutes(-5),
                EndedAt = factory.Time.GetUtcNow(),
            },
            ct);

    /// <summary>Oturum sonu dinleyicisini kaydeden sahte (yalnızca hangi kullanıcı için çağrıldığını tutar).</summary>
    private sealed class RecordingSessionsEndedListener : IUserSessionsEndedListener
    {
        private readonly List<Guid> _users = [];

        /// <summary>Gets çağrılan kullanıcılar.</summary>
        public IReadOnlyList<Guid> Users
        {
            get
            {
                lock (_users)
                {
                    return [.. _users];
                }
            }
        }

        /// <inheritdoc />
        public Task OnAllSessionsEndedAsync(Guid userId, CancellationToken cancellationToken)
        {
            lock (_users)
            {
                _users.Add(userId);
            }

            return Task.CompletedTask;
        }
    }
}
