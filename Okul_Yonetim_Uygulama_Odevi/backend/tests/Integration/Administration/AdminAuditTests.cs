using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>Spec 0005 AC-12: her yönetim işlemi audit'te (kim, ne, hangi kayıt, ne zaman, IP); geçici şifre log ve audit'te yok, veritabanında yalnızca özet.</summary>
[Trait("Category", "Integration")]
public sealed class AdminAuditTests
{
    private readonly TestDatabase database;

    public AdminAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task EveryAdminAction_IsAudited_AndTemporaryPasswordNeverLeaks()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.55");
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        DateTimeOffset started = DateTimeOffset.UtcNow.AddSeconds(-5);

        Guid branch = await CreateBranchAsync(client, token, "Denetim Şubesi");
        await StatusAsync(client, HttpMethod.Put, $"/api/admin/branches/{branch}", token, new { name = "Denetim Şubesi 2" });
        await StatusAsync(client, HttpMethod.Post, $"/api/admin/branches/{branch}/deactivate", token);
        await StatusAsync(client, HttpMethod.Post, $"/api/admin/branches/{branch}/activate", token);
        string email = UniqueEmail("denetim");
        var (_, added, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/members", token, NewMember(email, roles: Teacher(a.BranchId)));
        Guid userId = added.GetProperty("userId").GetGuid();
        string firstPassword = added.GetProperty("temporaryPassword").GetString()!;
        await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{userId}/profile", token, new { fullName = "Denetim Kişi", phone = (string?)null });
        await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{userId}/roles", token, new { roles = new[] { Teacher(branch) } });
        await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{userId}/deactivate", token);
        await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{userId}/activate", token);
        var (_, reset, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/members/{userId}/reset-password", token);
        string secondPassword = reset.GetProperty("temporaryPassword").GetString()!;

        List<AuditEntry> entries = await QueryAsync(factory, c => c.AuditEntries.Where(e => e.TenantId == a.TenantId && e.ActorUserId == a.FounderId && e.OccurredAt >= started && e.Action != AuditAction.LoginSucceeded).ToListAsync());
        entries.Select(e => e.Action).Distinct().ShouldBe(
            [AuditAction.BranchCreated, AuditAction.BranchUpdated, AuditAction.BranchDeactivated, AuditAction.BranchActivated, AuditAction.UserCreated,
             AuditAction.MemberAdded, AuditAction.RoleAssigned, AuditAction.MemberProfileUpdated, AuditAction.RoleRevoked, AuditAction.MemberDeactivated,
             AuditAction.MemberActivated, AuditAction.PasswordReset],
            ignoreOrder: true);
        entries.ShouldAllBe(e => e.IpAddress == "203.0.113.55" && e.TargetId != null && e.TargetType != null);
        entries.Single(e => e.Action == AuditAction.PasswordReset).TargetId.ShouldBe(userId);

        string log = factory.ReadLog();
        string audit = string.Join('\n', entries.Select(e => $"{e.AttemptedEmail}|{e.TargetType}|{e.IpAddress}|{e.Origin}"));
        string hash = await QueryAsync(factory, c => c.Users.Where(u => u.Id == userId).Select(u => u.PasswordHash).SingleAsync());
        foreach (string secret in new[] { firstPassword, secondPassword })
        {
            log.ShouldNotContain(secret);
            audit.ShouldNotContain(secret);
            hash.ShouldNotContain(secret);
        }

        log.ShouldNotContain(email);
    }
}
