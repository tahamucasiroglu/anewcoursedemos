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

/// <summary>Spec 0007 AC-2, AC-5: yetki ver/al ve doğrulama sıfırlama komutları.</summary>
[Trait("Category", "Integration")]
public sealed class PlatformAdminCliTests
{
    private readonly TestDatabase database;

    public PlatformAdminCliTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task GrantAndRevoke_ExistingAccount_ClosesSessions_AndAudits()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        string before = AccessToken(await LoginAsync(client, school.FounderEmail));

        (int exit, string output) = await CliAsync(factory, "grant-platform-admin", "--eposta", "  " + school.FounderEmail.ToUpperInvariant() + " ");
        exit.ShouldBe(0, output);
        output.ShouldContain("yetkisi verildi");
        (await QueryAsync(factory, c => c.Users.SingleAsync(u => u.Id == school.FounderId))).IsPlatformAdmin.ShouldBeTrue();
        (await StatusAsync(client, HttpMethod.Get, "/api/me", before)).ShouldBe(HttpStatusCode.Unauthorized, "verme açık oturumları kapatır");

        (await CliAsync(factory, "revoke-platform-admin", "--eposta", school.FounderEmail)).Exit.ShouldBe(0);
        (await QueryAsync(factory, c => c.Users.SingleAsync(u => u.Id == school.FounderId))).IsPlatformAdmin.ShouldBeFalse();
        JsonElement plain = await LoginAsync(client, school.FounderEmail);
        AccessToken(plain).ShouldNotBeNullOrEmpty("yetkisi alınan kişi yine kodsuz girer");

        List<AuditEntry> audit = await QueryAsync(factory, c => c.AuditEntries.Where(e => e.TargetId == school.FounderId && (e.Action == AuditAction.PlatformAdminGranted || e.Action == AuditAction.PlatformAdminRevoked)).ToListAsync());
        audit.Select(e => e.Action).ShouldBe([AuditAction.PlatformAdminGranted, AuditAction.PlatformAdminRevoked], ignoreOrder: true);
        audit.ShouldAllBe(e => e.Origin!.StartsWith("komut satırı: "));
    }

    [Fact]
    public async Task UnknownEmail_ChangesNothing()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        string email = UniqueEmail("yok");

        (int exit, string output) = await CliAsync(factory, "grant-platform-admin", "--eposta", email);

        exit.ShouldBe(1);
        output.ShouldContain("Bu e-postayla bir hesap yok");
        (await QueryAsync(factory, c => c.Users.AnyAsync(u => u.Email == email))).ShouldBeFalse();
    }

    [Theory]
    [InlineData("grant-platform-admin")]
    [InlineData("reset-two-factor")]
    public async Task MissingEmail_IsUsageError(string command)
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        (int exit, string output) = await CliAsync(factory, command);
        exit.ShouldBe(2);
        output.ShouldContain("Kullanım:");
    }

    [Fact]
    public async Task ResetTwoFactor_RequiresSetupAgain_ClosesSessions_AndAudits()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, string token, string secret) = await SignedInPlatformAdminAsync(factory, client, time);

        (int exit, string output) = await CliAsync(factory, "reset-two-factor", "--eposta", admin.FounderEmail);

        exit.ShouldBe(0, output);
        (await StatusAsync(client, HttpMethod.Get, Schools, token)).ShouldBe(HttpStatusCode.Unauthorized);
        User user = await QueryAsync(factory, c => c.Users.SingleAsync(u => u.Id == admin.FounderId));
        user.TwoFactorSecretCipher.ShouldBeNull();
        user.TwoFactorEnabledAt.ShouldBeNull();
        JsonElement login = await LoginAsync(client, admin.FounderEmail);
        login.GetProperty("twoFactorSetupRequired").GetBoolean().ShouldBeTrue();
        var verify = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge = login.GetProperty("twoFactorChallenge").GetString(), code = Code(secret, time, 1) });
        verify.Status.ShouldBe(HttpStatusCode.Unauthorized, "eski anahtarın kodu artık geçmez");
        (await QueryAsync(factory, c => c.AuditEntries.CountAsync(e => e.Action == AuditAction.TwoFactorReset && e.TargetId == admin.FounderId && e.Origin!.StartsWith("komut satırı: ")))).ShouldBe(1);
    }
}
