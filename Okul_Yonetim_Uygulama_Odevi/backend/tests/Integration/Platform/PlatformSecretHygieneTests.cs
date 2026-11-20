using System.Net;
using System.Text.RegularExpressions;
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

/// <summary>Spec 0007 AC-11: anahtar şifreli; anahtar, kod, geçici şifre ve şifre log/yanıt/audit'te yok; anahtar ayarsızsa güvenli ret.</summary>
[Trait("Category", "Integration")]
public sealed class PlatformSecretHygieneTests
{
    private readonly TestDatabase database;

    public PlatformSecretHygieneTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Secrets_NeverAppearInLogsAuditOrDatabaseInPlain()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, string token, string secret) = await SignedInPlatformAdminAsync(factory, client, time);
        string setupCode = Code(secret, time);
        time.Advance(TimeSpan.FromSeconds(30));
        string challenge = await ChallengeAsync(client, admin.FounderEmail);
        string wrong = Code(secret, time) == "000000" ? "111111" : "000000";
        var bad = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = wrong });
        string loginCode = Code(secret, time);
        var good = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = loginCode });
        var created = await CallAsync(client, HttpMethod.Post, Schools, token, new { schoolName = $"Gizli Okul {Guid.NewGuid():N}", branchName = "Merkez", founderName = "Gizli Kurucu", founderEmail = UniqueEmail("gizli") });
        string temporary = created.Body.GetProperty("temporaryPassword").GetString()!;

        string log = factory.ReadLog();
        string audit = string.Join('\n', await QueryAsync(factory, c => c.AuditEntries.Select(e => (e.AttemptedEmail ?? string.Empty) + "|" + (e.Origin ?? string.Empty) + "|" + (e.TargetType ?? string.Empty)).ToListAsync()));
        string cipher = (await QueryAsync(factory, c => c.Users.SingleAsync(u => u.Id == admin.FounderId))).TwoFactorSecretCipher!;
        string[] secrets = [secret, setupCode, loginCode, wrong, temporary, Password, challenge];
        foreach (string value in secrets)
        {
            log.ShouldNotContain(value);
            audit.ShouldNotContain(value);
        }

        bad.Raw.ShouldNotContain(secret);
        good.Raw.ShouldNotContain(secret);
        cipher.ShouldNotContain(secret);
        (await QueryAsync(factory, c => c.TwoFactorChallenges.AnyAsync(x => x.TokenHash == challenge))).ShouldBeFalse("bekleme anahtarı yalnızca özetiyle saklanır");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("bozuk-anahtar")]
    public async Task MissingOrMalformedEncryptionKey_RejectsPlatformAdminSafely_OthersStillSignIn(string? key)
    {
        string adminEmail;
        using (OkulApiFactory configured = new(this.database.ConnectionString))
        {
            adminEmail = (await NewPlatformAdminAsync(configured)).FounderEmail;
        }

        using OkulApiFactory factory = new(this.database.ConnectionString, twoFactorKey: key);
        using HttpClient client = factory.CreateClient();
        SchoolSeed plain = await CreateSchoolAsync(factory);

        using HttpResponseMessage admin = await PostLoginAsync(client, adminEmail, Password);
        string raw = await admin.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        admin.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        Regex.Unescape(raw).ShouldContain("İki adımlı doğrulama yapılandırılmamış");
        AccessToken(await LoginAsync(client, plain.FounderEmail)).ShouldNotBeNullOrEmpty();
        factory.ReadLog().ShouldContain("iki adımlı doğrulama yapılandırılmamış");
        if (key is not null)
        {
            factory.ReadLog().ShouldNotContain(key);
        }
    }
}
