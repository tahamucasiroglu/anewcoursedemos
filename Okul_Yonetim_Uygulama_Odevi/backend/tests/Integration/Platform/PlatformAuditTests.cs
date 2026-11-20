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

/// <summary>Spec 0007 AC-12: kurulum, doğru/yanlış kod ve kilit audit'e kim, ne zaman, IP ile yazılır.</summary>
[Trait("Category", "Integration")]
public sealed class PlatformAuditTests
{
    private readonly TestDatabase database;

    public PlatformAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task TwoFactorEvents_AreAudited_WithActorAndIp()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.88");
        (SchoolSeed admin, _, string secret) = await SignedInPlatformAdminAsync(factory, client, time);
        await SignInWithCodeAsync(client, time, admin.FounderEmail, secret);
        time.Advance(TimeSpan.FromSeconds(30));
        string challenge = await ChallengeAsync(client, admin.FounderEmail);
        string wrong = Code(secret, time) == "000000" ? "111111" : "000000";
        for (int i = 0; i < 3; i++)
        {
            await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = wrong });
        }

        List<AuditEntry> audit = await QueryAsync(factory, c => c.AuditEntries.Where(e => e.ActorUserId == admin.FounderId).ToListAsync());
        audit.Count(e => e.Action == AuditAction.TwoFactorEnabled).ShouldBe(1);
        audit.Count(e => e.Action == AuditAction.TwoFactorSucceeded).ShouldBe(1);
        audit.Count(e => e.Action == AuditAction.TwoFactorFailed).ShouldBe(3);
        audit.Count(e => e.Action == AuditAction.LoginLockedOut).ShouldBe(1);
        audit.Where(e => e.Action is AuditAction.TwoFactorEnabled or AuditAction.TwoFactorSucceeded or AuditAction.TwoFactorFailed or AuditAction.LoginLockedOut)
            .ShouldAllBe(e => e.IpAddress == "203.0.113.88");
    }
}
