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

/// <summary>Spec 0007 AC-3: ilk girişte kurulum; doğru kodla tamamlanır; anahtar bir daha alınamaz.</summary>
[Trait("Category", "Integration")]
public sealed class TwoFactorSetupTests
{
    private readonly TestDatabase database;

    public TwoFactorSetupTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FirstLogin_RequiresSetup_WrongCodeFails_RightCodeOpensPanel()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed admin = await NewPlatformAdminAsync(factory);

        JsonElement login = await LoginAsync(client, admin.FounderEmail);
        login.GetProperty("twoFactorSetupRequired").GetBoolean().ShouldBeTrue();
        login.GetProperty("accessToken").GetString().ShouldBeNullOrEmpty();
        login.GetProperty("refreshToken").ValueKind.ShouldBe(JsonValueKind.Null);
        string challenge = login.GetProperty("twoFactorChallenge").GetString()!;
        (await StatusAsync(client, HttpMethod.Get, Schools, challenge)).ShouldBe(HttpStatusCode.Unauthorized, "bekleme anahtarı oturum değildir");

        var setup = await PostAsync(client, "/api/auth/two-factor/setup", new { challenge });
        setup.Status.ShouldBe(HttpStatusCode.OK);
        string secret = setup.Body.GetProperty("secret").GetString()!;
        secret.ShouldMatch("^[A-Z2-7]{32}$");
        setup.Body.GetProperty("otpAuthUri").GetString()!.ShouldStartWith("otpauth://totp/");
        (await PostAsync(client, "/api/auth/two-factor/setup", new { challenge })).Body.GetProperty("secret").GetString().ShouldBe(secret, "aynı bekleme içinde anahtar değişmez");

        string wrong = Code(secret, time) == "000000" ? "111111" : "000000";
        var bad = await PostAsync(client, "/api/auth/two-factor/setup/confirm", new { challenge, code = wrong });
        bad.Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(bad.Body).ShouldBe("Kod hatalı");
        (await QueryAsync(factory, c => c.Users.SingleAsync(u => u.Id == admin.FounderId))).TwoFactorEnabledAt.ShouldBeNull();

        var ok = await PostAsync(client, "/api/auth/two-factor/setup/confirm", new { challenge, code = Code(secret, time) });
        ok.Status.ShouldBe(HttpStatusCode.OK, ok.Raw);
        (await StatusAsync(client, HttpMethod.Get, Schools, AccessToken(ok.Body))).ShouldBe(HttpStatusCode.OK);
        (await PostAsync(client, "/api/auth/two-factor/setup/confirm", new { challenge, code = Code(secret, time) })).Status.ShouldBe(HttpStatusCode.Unauthorized, "bekleme tek kullanımlık");

        User user = await QueryAsync(factory, c => c.Users.SingleAsync(u => u.Id == admin.FounderId));
        user.TwoFactorEnabledAt.ShouldNotBeNull();
        user.TwoFactorSecretCipher.ShouldNotBeNull();
        user.TwoFactorSecretCipher.ShouldNotContain(secret);
    }

    [Fact]
    public async Task AfterSetup_SecretCannotBeFetchedAgain()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, _, string secret) = await SignedInPlatformAdminAsync(factory, client, time);

        JsonElement again = await LoginAsync(client, admin.FounderEmail);
        again.GetProperty("twoFactorSetupRequired").GetBoolean().ShouldBeFalse();
        var setup = await PostAsync(client, "/api/auth/two-factor/setup", new { challenge = again.GetProperty("twoFactorChallenge").GetString() });

        setup.Status.ShouldBe(HttpStatusCode.Unauthorized);
        setup.Raw.ShouldNotContain(secret);
    }

    [Fact]
    public async Task ExpiredChallenge_IsRejected()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed admin = await NewPlatformAdminAsync(factory);
        string challenge = await ChallengeAsync(client, admin.FounderEmail);

        time.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
        var setup = await PostAsync(client, "/api/auth/two-factor/setup", new { challenge });

        setup.Status.ShouldBe(HttpStatusCode.Unauthorized);
        Detail(setup.Body).ShouldBe("Doğrulama süresi doldu, yeniden giriş yapın.");
    }
}
