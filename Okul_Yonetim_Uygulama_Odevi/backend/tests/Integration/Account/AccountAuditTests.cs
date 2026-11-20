using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Account.AccountTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Account;

/// <summary>Spec 0006 AC-12: profil, şifre değiştirme (başarılı/yanlış), yenileme isteği (kayıtlı/kayıtsız), bağlantıyla yenileme ve geçersiz bağlantı audit'te.</summary>
[Trait("Category", "Integration")]
public sealed class AccountAuditTests
{
    private readonly TestDatabase database;

    public AccountAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task AccountActions_AreAudited_WithActorOrAttemptedEmailAndIp()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.66");
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("denetim");
        Guid personId = await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        string unknown = UniqueEmail("kayitsiz");
        string access = AccessToken(await LoginAsync(client, person));

        await StatusAsync(client, HttpMethod.Put, "/api/account/profile", access, new { fullName = "Denetim Kişi", phone = (string?)null });
        await StatusAsync(client, HttpMethod.Post, "/api/account/change-password", access, new { currentPassword = "Yanlis-Sifre-1", newPassword = NewPassword, newPasswordRepeat = NewPassword });
        await StatusAsync(client, HttpMethod.Post, "/api/account/change-password", access, new { currentPassword = Password, newPassword = NewPassword, newPasswordRepeat = NewPassword });
        await ForgotAsync(client, person);
        await ForgotAsync(client, unknown);
        int invalidBefore = await QueryAsync(factory, c => c.AuditEntries.CountAsync(e => e.Action == AuditAction.PasswordResetLinkInvalid && e.IpAddress == "203.0.113.66"));
        await ResetAsync(client, "uydurma-anahtar-0123456789");
        int invalidAfter = await QueryAsync(factory, c => c.AuditEntries.CountAsync(e => e.Action == AuditAction.PasswordResetLinkInvalid && e.IpAddress == "203.0.113.66"));
        await ResetAsync(client, TokenFrom(outbox.To(person).Single()), "Son-Sifre-2026");

        List<AuditEntry> mine = await QueryAsync(factory, c => c.AuditEntries.Where(e => e.ActorUserId == personId).ToListAsync());
        foreach (AuditAction expected in new[] { AuditAction.ProfileUpdated, AuditAction.PasswordChangeFailed, AuditAction.PasswordChanged, AuditAction.PasswordResetRequested, AuditAction.PasswordResetCompleted })
        {
            mine.Select(e => e.Action).ShouldContain(expected);
        }
        mine.Where(e => e.Action != AuditAction.LoginSucceeded).ShouldAllBe(e => e.IpAddress == "203.0.113.66");
        (await QueryAsync(factory, c => c.AuditEntries.CountAsync(e => e.Action == AuditAction.PasswordResetRequested && e.AttemptedEmail == unknown && e.IpAddress == "203.0.113.66"))).ShouldBe(1);
        (invalidAfter - invalidBefore).ShouldBe(1, "geçersiz bağlantı denemesi tam bir audit kaydı");
    }
}
