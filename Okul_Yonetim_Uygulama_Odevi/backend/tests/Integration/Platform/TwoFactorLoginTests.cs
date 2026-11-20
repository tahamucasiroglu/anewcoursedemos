using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Platform.PlatformTestData;

namespace OkulYonetim.Tests.Integration.Platform;

/// <summary>Spec 0007 AC-4: kurulu hesapta kodla giriş, saat kayması, kilit, aynı kodun reddi; diğer kişilerde kod yok.</summary>
[Trait("Category", "Integration")]
public sealed class TwoFactorLoginTests
{
    private readonly TestDatabase database;

    public TwoFactorLoginTests(TestDatabase database)
    {
        this.database = database;
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CodeFromAdjacentStep_IsAccepted(int offset)
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, _, string secret) = await SignedInPlatformAdminAsync(factory, client, time);
        time.Advance(TimeSpan.FromSeconds(90));

        string challenge = await ChallengeAsync(client, admin.FounderEmail);
        var verified = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = Code(secret, time, offset) });

        verified.Status.ShouldBe(HttpStatusCode.OK, verified.Raw);
        (await StatusAsync(client, HttpMethod.Get, Schools, AccessToken(verified.Body))).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CodeTwoStepsAway_IsRejected()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, _, string secret) = await SignedInPlatformAdminAsync(factory, client, time);
        time.Advance(TimeSpan.FromSeconds(90));

        string challenge = await ChallengeAsync(client, admin.FounderEmail);
        var verified = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = Code(secret, time, 2) });

        verified.Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(verified.Body).ShouldBe("Kod hatalı");
    }

    [Fact]
    public async Task SameCode_IsNotAcceptedTwice()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, _, string secret) = await SignedInPlatformAdminAsync(factory, client, time);
        time.Advance(TimeSpan.FromSeconds(30));
        string code = Code(secret, time);

        var first = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge = await ChallengeAsync(client, admin.FounderEmail), code });
        var second = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge = await ChallengeAsync(client, admin.FounderEmail), code });

        first.Status.ShouldBe(HttpStatusCode.OK);
        second.Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(second.Body).ShouldBe("Kod hatalı");
    }

    [Fact]
    public async Task ThreeWrongCodes_LockAccount_EvenRightCodeAndPasswordRejected()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, _, string secret) = await SignedInPlatformAdminAsync(factory, client, time);
        time.Advance(TimeSpan.FromSeconds(30));
        string challenge = await ChallengeAsync(client, admin.FounderEmail);
        string wrong = Code(secret, time) == "000000" ? "111111" : "000000";

        var r1 = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = wrong });
        var r2 = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = wrong });
        var r3 = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = wrong });
        var right = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = Code(secret, time) });

        Detail(r1.Body).ShouldBe("Kod hatalı");
        Detail(r2.Body).ShouldBe("Kod hatalı");
        Detail(r3.Body).ShouldBe("Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin.");
        right.Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(right.Body).ShouldBe(Detail(r3.Body));
        using HttpResponseMessage password = await PostLoginAsync(client, admin.FounderEmail, Password);
        password.StatusCode.ShouldNotBe(HttpStatusCode.OK, "şifre hatalarıyla aynı sayaç: kilitliyken şifre de reddedilir");

        time.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(30));
        (await SignInWithCodeAsync(client, time, admin.FounderEmail, secret)).GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();
    }

    /// <summary>Review round 1, H-1: araya doğru şifreyle yeni giriş girmek kod sayacını sıfırlamaz.</summary>
    [Fact]
    public async Task CorrectPasswordBetweenWrongCodes_DoesNotResetLock()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, _, string secret) = await SignedInPlatformAdminAsync(factory, client, time);
        time.Advance(TimeSpan.FromSeconds(30));
        string wrong = Code(secret, time) == "000000" ? "111111" : "000000";

        string first = await ChallengeAsync(client, admin.FounderEmail);
        await PostAsync(client, "/api/auth/two-factor/verify", new { challenge = first, code = wrong });
        await PostAsync(client, "/api/auth/two-factor/verify", new { challenge = first, code = wrong });
        string second = await ChallengeAsync(client, admin.FounderEmail);
        var third = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge = second, code = wrong });
        var right = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge = second, code = Code(secret, time) });

        Detail(third.Body).ShouldBe("Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin.");
        right.Status.ShouldBe(HttpStatusCode.BadRequest, "kilitliyken doğru kod da reddedilir");
    }

    [Fact]
    public async Task WrongPasswordsCount_TowardsSameLock()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, _, string secret) = await SignedInPlatformAdminAsync(factory, client, time);
        time.Advance(TimeSpan.FromSeconds(30));
        string challenge = await ChallengeAsync(client, admin.FounderEmail);
        string wrong = Code(secret, time) == "000000" ? "111111" : "000000";

        (await PostLoginAsync(client, admin.FounderEmail, "Yanlis-Sifre-1")).Dispose();
        (await PostLoginAsync(client, admin.FounderEmail, "Yanlis-Sifre-2")).Dispose();
        var third = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = wrong });

        Detail(third.Body).ShouldBe("Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin.");
    }

    [Fact]
    public async Task NonPlatformAdmin_LoginIsUnchanged()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);

        JsonElement login = await LoginAsync(client, school.FounderEmail);

        AccessToken(login).ShouldNotBeNullOrEmpty();
        login.GetProperty("twoFactorChallenge").ValueKind.ShouldBe(JsonValueKind.Null);
        login.GetProperty("twoFactorSetupRequired").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task PlatformAdminSession_KeepsSchoolScreens_AndSurvivesRefresh()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, string token, string secret) = await SignedInPlatformAdminAsync(factory, client, time);
        JsonElement auth = await SignInWithCodeAsync(client, time, admin.FounderEmail, secret);

        (await StatusAsync(client, HttpMethod.Get, "/api/admin/members", AccessToken(auth))).ShouldBe(HttpStatusCode.OK, "kendi okulunun kurucu ekranları açılır");
        var refreshed = await PostAsync(client, "/api/auth/refresh", new { refreshToken = auth.GetProperty("refreshToken").GetString() });
        refreshed.Status.ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Get, Schools, AccessToken(refreshed.Body))).ShouldBe(HttpStatusCode.OK, "yenilenen oturum kodla açılmış sayılır");
        token.ShouldNotBeNullOrEmpty();
    }
}
