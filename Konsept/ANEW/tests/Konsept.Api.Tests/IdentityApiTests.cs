using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Identity.Application;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Kimlik uçları uçtan uca, hızlı profil (bellek depoları, ilerletilebilir saat): kayıt, giriş, kilit, hız sınırı, kayan
/// oturum, çıkış ve olay sahipliği (spec 0002 AC-1, AC-2, AC-4, AC-5, AC-7, AC-8, AC-9, AC-10, AC-13, AC-14, AC-15, AC-16).
/// </summary>
public sealed class IdentityApiTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="IdentityApiTests"/> class.</summary>
    /// <param name="factory">Kimlik test fabrikası.</param>
    public IdentityApiTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Geçerli kayıt token çifti ve adı döndürür; kayıt zamanı (UTC) yazılır, şifre düz metin saklanmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_Valid_ReturnsSessionAndStoresRegisteredAt()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = AuthenticatedClient.NewEmail();

        var session = await AuthenticatedClient.RegisterAsync(client, _factory.Services, email, "Sifre1234", " Ayşe Yılmaz ", ct);

        session.GetProperty("name").GetString().ShouldBe("Ayşe Yılmaz");
        session.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        session.GetProperty("refreshToken").GetString()!.ShouldStartWith("knr_");
        var user = _factory.Users.Get(email);
        user.RegisteredAt.ShouldBe(_factory.Time.GetUtcNow());
        user.PasswordHash.ShouldNotContain("Sifre1234");
        await AssertSessionAsync(client, session, HttpStatusCode.OK, ct);
    }

    /// <summary>Kayıtlı e-posta başka harf biçimiyle (tr-TR) ikinci kez kaydedilemez; ikinci hesap oluşmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_DuplicateEmailOtherCase_ConflictNoSecondAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var local = "irem" + Guid.NewGuid().ToString("N")[..8];
        await AuthenticatedClient.RegisterAsync(client, _factory.Services, $"{local}@x.com", "Sifre1234", "İrem", ct);

        using var second = await AuthenticatedClient.PostAsync(
            client, _factory.Services, "/api/auth/register", new { name = "Başka", email = $"{local.ToUpperInvariant()}@X.COM", password = "Sifre1234" }, ct);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>(ct);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe(IdentityErrors.EmailTakenCode);
        problem.GetProperty("title").GetString().ShouldBe("Bu e-posta zaten kayıtlı.");
        _factory.Users.Users.Count(u => u.NormalizedEmail == $"{local}@x.com".ToUpperInvariant()).ShouldBe(1);
    }

    /// <summary>Doğru şifreyle giriş son giriş zamanını günceller.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_Valid_UpdatesLastLogin()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = await RegisterNewAsync(client, ct);
        _factory.Time.Advance(TimeSpan.FromMinutes(3));

        var session = await AuthenticatedClient.LoginAsync(client, _factory.Services, email, AuthenticatedClient.Password, ct);

        _factory.Users.Get(email).LastLoginAt.ShouldBe(_factory.Time.GetUtcNow());
        await AssertSessionAsync(client, session, HttpStatusCode.OK, ct);
    }

    /// <summary>Yanlış şifre: "2 hak", "1 hak", 3.'de 15 dakika kilit ve açılma zamanı.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Lockout_WrongSequence_TwoOneThenLocked15Min()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = await RegisterNewAsync(client, ct);

        var first = await LoginProblemAsync(client, email, "Yanlis123", ct);
        var second = await LoginProblemAsync(client, email, "Yanlis123", ct);
        var third = await LoginProblemAsync(client, email, "Yanlis123", ct);

        first.GetProperty("title").GetString().ShouldBe("Yanlış girdiniz, 2 hakkınız kaldı");
        first.GetProperty("remainingAttempts").GetInt32().ShouldBe(2);
        second.GetProperty("title").GetString().ShouldBe("Yanlış girdiniz, 1 hakkınız kaldı");
        second.GetProperty("remainingAttempts").GetInt32().ShouldBe(1);
        third.GetProperty("code").GetString().ShouldBe(IdentityErrors.LockedCode);
        third.GetProperty("lockedUntil").GetDateTimeOffset().ShouldBe(_factory.Time.GetUtcNow().AddMinutes(15));
    }

    /// <summary>
    /// Kilitliyken doğru şifre reddedilir; yanlış deneme açılma zamanını değiştirmez; 15 dakika sonra doğru şifre girer;
    /// kilit sonrası ilk yanlışta yeniden "2 hak".
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Lockout_WhileLocked_CorrectRejected_WrongNotExtending_FreshThreeAfterExpiry()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = await RegisterNewAsync(client, ct);
        await LoginProblemAsync(client, email, "Yanlis123", ct);
        await LoginProblemAsync(client, email, "Yanlis123", ct);
        var locked = await LoginProblemAsync(client, email, "Yanlis123", ct);
        var until = locked.GetProperty("lockedUntil").GetDateTimeOffset();

        _factory.Time.Advance(TimeSpan.FromMinutes(5));
        var correctWhileLocked = await LoginProblemAsync(client, email, AuthenticatedClient.Password, ct);
        _factory.Time.Advance(TimeSpan.FromMinutes(5));
        var wrongWhileLocked = await LoginProblemAsync(client, email, "Yanlis123", ct);
        _factory.Time.Advance(TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(1)));
        var opened = await AuthenticatedClient.LoginAsync(client, _factory.Services, email, AuthenticatedClient.Password, ct);
        var afterUnlock = await LoginProblemAsync(client, email, "Yanlis123", ct);

        correctWhileLocked.GetProperty("code").GetString().ShouldBe(IdentityErrors.LockedCode);
        correctWhileLocked.GetProperty("lockedUntil").GetDateTimeOffset().ShouldBe(until);
        wrongWhileLocked.GetProperty("code").GetString().ShouldBe(IdentityErrors.LockedCode);
        wrongWhileLocked.GetProperty("lockedUntil").GetDateTimeOffset().ShouldBe(until);
        _factory.Users.Get(email).LockedUntil.ShouldBe(until);
        opened.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        afterUnlock.GetProperty("remainingAttempts").GetInt32().ShouldBe(2);
    }

    /// <summary>2 yanlıştan sonra başarılı giriş sayacı sıfırlar: sonraki yanlış yine "2 hak".</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Lockout_SuccessAfterTwoFailures_ResetsCounter()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = await RegisterNewAsync(client, ct);
        await LoginProblemAsync(client, email, "Yanlis123", ct);
        await LoginProblemAsync(client, email, "Yanlis123", ct);

        await AuthenticatedClient.LoginAsync(client, _factory.Services, email, AuthenticatedClient.Password, ct);
        var next = await LoginProblemAsync(client, email, "Yanlis123", ct);

        next.GetProperty("remainingAttempts").GetInt32().ShouldBe(2);
    }

    /// <summary>Kayıtlı olmayan e-posta: genel mesaj, kalan hak yok; hiçbir sayaç ve olay değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_UnknownEmail_GenericMessage_NoCounterNoEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        await RegisterNewAsync(client, ct);
        var counters = _factory.Users.Users.Select(u => (u.Id, u.FailedLoginCount)).ToList();
        var eventCount = _factory.Events.Events.Count;

        var problem = await LoginProblemAsync(client, AuthenticatedClient.NewEmail(), "Yanlis123", ct);

        problem.GetProperty("code").GetString().ShouldBe(IdentityErrors.InvalidCredentialsCode);
        problem.GetProperty("title").GetString().ShouldBe("E-posta ya da şifre hatalı");
        problem.TryGetProperty("remainingAttempts", out _).ShouldBeFalse();
        _factory.Users.Users.Select(u => (u.Id, u.FailedLoginCount)).ShouldBe(counters, ignoreOrder: true);
        _factory.Events.Events.Count.ShouldBe(eventCount);
    }

    /// <summary>Boş alanla giriş alan hatası verir ve hak sayacını değiştirmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_EmptyFields_FieldErrors_NoCounterChange()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = await RegisterNewAsync(client, ct);

        using var response = await AuthenticatedClient.PostAsync(client, _factory.Services, "/api/auth/login", new { email, password = string.Empty }, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.GetProperty("errors").GetProperty("Password")[0].GetString().ShouldBe("Şifrenizi girin.");
        _factory.Users.Get(email).FailedLoginCount.ShouldBe(0);
    }

    /// <summary>
    /// Aynı IP'den bir dakikada 11. giriş/kayıt denemesi, kimlik bilgileri doğru olsa bile 429 "Çok fazla deneme" ile
    /// reddedilir ve hiçbir hak sayacı değişmez; 1 dakika sonra yeniden mümkün olur.
    /// </summary>
    /// <param name="endpoint">Uç (<c>login</c> ya da <c>register</c>).</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("login")]
    [InlineData("register")]
    public async Task RateLimit_EleventhPerIp_Rejected_OpensAfterOneMinute_NoCounterChange(string endpoint)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = await RegisterNewAsync(client, ct);
        var ip = AuthenticatedClient.NewIp();
        for (var i = 0; i < 10; i++)
        {
            using var allowed = await AuthenticatedClient.PostAsync(client, _factory.Services, $"/api/auth/{endpoint}", Body(endpoint, AuthenticatedClient.NewEmail()), ct, ip);
            allowed.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        }

        using var rejected = await AuthenticatedClient.PostAsync(client, _factory.Services, $"/api/auth/{endpoint}", Body(endpoint, endpoint == "login" ? email : AuthenticatedClient.NewEmail()), ct, ip);
        var problem = await rejected.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (endpoint == "login")
        {
            // Hız sınırına takılan yanlış şifreli deneme de hak sayacına/kilide dokunmaz (review R4-2, AC-10).
            using var rejectedWrong = await AuthenticatedClient.PostAsync(client, _factory.Services, "/api/auth/login", new { email, password = "Yanlis12345" }, ct, ip);
            rejectedWrong.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
            _factory.Users.Get(email).FailedLoginCount.ShouldBe(0);
            _factory.Users.Get(email).LockedUntil.ShouldBeNull();
        }

        _factory.Time.Advance(TimeSpan.FromSeconds(61));
        using var reopened = await AuthenticatedClient.PostAsync(client, _factory.Services, $"/api/auth/{endpoint}", Body(endpoint, endpoint == "login" ? email : AuthenticatedClient.NewEmail()), ct, ip);

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        problem.GetProperty("code").GetString().ShouldBe(IdentityErrors.TooManyAttemptsCode);
        problem.GetProperty("title").GetString().ShouldBe("Çok fazla deneme, biraz sonra tekrar deneyin");
        _factory.Users.Get(email).FailedLoginCount.ShouldBe(0);
        reopened.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// İletilen istemci IP başlığına yalnızca servis anahtarıyla güvenilir: anahtarsız istekte her seferinde farklı IP
    /// yazılsa da hepsi bağlantının adresinden sayılır ve 11. istek reddedilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RateLimit_ForwardedIp_TrustedOnlyWithServiceKey()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        _factory.Time.Advance(TimeSpan.FromMinutes(3));
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 11; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { email = AuthenticatedClient.NewEmail(), password = "Yanlis123" }),
            };
            request.Headers.Add("X-Konsept-Client-Ip", AuthenticatedClient.NewIp());
            using var response = await client.SendAsync(request, ct);
            statuses.Add(response.StatusCode);
        }

        statuses.Take(10).ShouldAllBe(s => s == HttpStatusCode.Unauthorized);
        statuses[10].ShouldBe(HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// Oturum son kullanımdan itibaren 14 gün geçerli ve kullanıldıkça uzar (saat ilerletilerek): 13'er gün arayla yenileme
    /// çalışır; 14 günden uzun kullanılmayan oturum reddedilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Session_SlidingExpiry_14Days()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = await RegisterNewAsync(client, ct);
        var session = await AuthenticatedClient.LoginAsync(client, _factory.Services, email, AuthenticatedClient.Password, ct);

        _factory.Time.Advance(TimeSpan.FromDays(13));
        var first = await RefreshAsync(client, session, ct);
        _factory.Time.Advance(TimeSpan.FromDays(13));
        var second = await RefreshAsync(client, first.Body, ct);
        await AssertSessionAsync(client, second.Body, HttpStatusCode.OK, ct);
        _factory.Time.Advance(TimeSpan.FromDays(14).Add(TimeSpan.FromMinutes(1)));
        var expired = await RefreshAsync(client, second.Body, ct);

        first.Status.ShouldBe(HttpStatusCode.OK);
        second.Status.ShouldBe(HttpStatusCode.OK);
        expired.Status.ShouldBe(HttpStatusCode.Unauthorized);
        expired.Body.GetProperty("code").GetString().ShouldBe(IdentityErrors.SessionInvalidCode);
    }

    /// <summary>
    /// İki oturumdan birinden çıkış: tüm oturumlar iptal; diğer oturumun hâlâ süresi dolmamış erişim token'ı ve her iki refresh
    /// token reddedilir; dönmüş eski refresh token da ikinci kez kullanılamaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Logout_RevokesAllSessions_OtherAccessAndRefreshRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = await RegisterNewAsync(client, ct);
        var browserA = await AuthenticatedClient.LoginAsync(client, _factory.Services, email, AuthenticatedClient.Password, ct);
        var browserB = await AuthenticatedClient.LoginAsync(client, _factory.Services, email, AuthenticatedClient.Password, ct);
        var rotated = await RefreshAsync(client, browserB, ct);
        var reused = await RefreshAsync(client, browserB, ct);

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Authorization = Bearer(browserA);
        using var logoutResponse = await client.SendAsync(logout, ct);

        reused.Status.ShouldBe(HttpStatusCode.Unauthorized);
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await logoutResponse.Content.ReadFromJsonAsync<int>(ct)).ShouldBe(3); // kayıt + iki tarayıcı
        await AssertSessionAsync(client, rotated.Body, HttpStatusCode.Unauthorized, ct);
        await AssertSessionAsync(client, browserA, HttpStatusCode.Unauthorized, ct);
        (await RefreshAsync(client, rotated.Body, ct)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await RefreshAsync(client, browserA, ct)).Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Kimlik olayları kullanıcıya bağlı yazılır (kayıt, başarılı/başarısız giriş, çıkış); B'nin Günlük şeridinde A'nın
    /// olayı görünmez; kayıtlı olmayan e-postayla deneme olay yazmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AuthEvents_BoundToUser_StripShowsOnlyOwn()
    {
        var ct = TestContext.Current.CancellationToken;
        using var anonymous = _factory.CreateClient();
        var emailA = AuthenticatedClient.NewEmail();
        using var clientB = await AuthenticatedClient.CreateAsync(_factory, ct, name: "B");
        var sessionA = await AuthenticatedClient.RegisterAsync(anonymous, _factory.Services, emailA, AuthenticatedClient.Password, "A", ct);
        var userA = sessionA.GetProperty("userId").GetGuid();
        await LoginProblemAsync(anonymous, emailA, "Yanlis123", ct);
        var loginA = await AuthenticatedClient.LoginAsync(anonymous, _factory.Services, emailA, AuthenticatedClient.Password, ct);
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Authorization = Bearer(loginA);
        (await anonymous.SendAsync(logout, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var before = _factory.Events.Events.Count;
        await LoginProblemAsync(anonymous, AuthenticatedClient.NewEmail(), "Yanlis123", ct);

        var statusB = await clientB.GetFromJsonAsync<JsonElement>("/api/system/status", ct);
        var userB = (await clientB.GetFromJsonAsync<JsonElement>("/api/auth/session", ct)).GetProperty("userId").GetGuid();

        _factory.Events.Events.Where(e => e.UserId == userA).Select(e => e.Type).ShouldBe(
            [IdentityEvents.Registered, IdentityEvents.LoginFailed, IdentityEvents.LoginSucceeded, IdentityEvents.LoggedOut]);
        _factory.Events.Events.Count.ShouldBe(before);
        var lastEvent = statusB.GetProperty("lastEvent");
        lastEvent.GetProperty("userId").GetGuid().ShouldNotBe(userA);
        lastEvent.GetProperty("userId").GetGuid().ShouldBe(userB);
    }

    private static object Body(string endpoint, string email) =>
        endpoint == "login"
            ? new { email, password = AuthenticatedClient.Password }
            : new { name = "Deneme", email, password = AuthenticatedClient.Password };

    private static AuthenticationHeaderValue Bearer(JsonElement session) =>
        new("Bearer", session.GetProperty("accessToken").GetString());

    private static async Task AssertSessionAsync(HttpClient client, JsonElement session, HttpStatusCode expected, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/session");
        request.Headers.Authorization = Bearer(session);
        using var response = await client.SendAsync(request, ct);
        response.StatusCode.ShouldBe(expected);
        if (expected == HttpStatusCode.OK)
        {
            var user = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            user.GetProperty("userId").GetGuid().ShouldBe(session.GetProperty("userId").GetGuid());
        }
    }

    private async Task<string> RegisterNewAsync(HttpClient client, CancellationToken ct)
    {
        var email = AuthenticatedClient.NewEmail();
        await AuthenticatedClient.RegisterAsync(client, _factory.Services, email, AuthenticatedClient.Password, "Deneme", ct);
        return email;
    }

    private async Task<JsonElement> LoginProblemAsync(HttpClient client, string email, string password, CancellationToken ct)
    {
        using var response = await AuthenticatedClient.PostAsync(client, _factory.Services, "/api/auth/login", new { email, password }, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> RefreshAsync(HttpClient client, JsonElement session, CancellationToken ct)
    {
        using var response = await AuthenticatedClient.PostAsync(
            client, _factory.Services, "/api/auth/refresh", new { refreshToken = session.GetProperty("refreshToken").GetString() }, ct);
        return (response.StatusCode, await response.Content.ReadFromJsonAsync<JsonElement>(ct));
    }
}
