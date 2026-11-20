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

/// <summary>Spec 0006 AC-11: şifreler ve yenileme bağlantısı log, hata yanıtı ve audit'te yok.</summary>
[Trait("Category", "Integration")]
public sealed class AccountSecretHygieneTests
{
    private readonly TestDatabase database;

    public AccountSecretHygieneTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task PasswordsAndLinkToken_NeverInLogsResponsesOrAudit()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string person = UniqueEmail("gizli");
        await AddMemberAsync(factory, person, a.TenantId, Role.Teacher, a.BranchId);
        string access = AccessToken(await LoginAsync(client, person));
        const string Changed = "Degisen-Gizli-2026";
        const string Reset = "Yenilenen-Gizli-2026";
        List<string> responses = [];

        responses.Add((await CallAsync(client, HttpMethod.Post, "/api/account/change-password", access, new { currentPassword = "Yanlis-Gizli-1", newPassword = Changed, newPasswordRepeat = Changed })).Raw);
        responses.Add((await CallAsync(client, HttpMethod.Post, "/api/account/change-password", access, new { currentPassword = Password, newPassword = Changed, newPasswordRepeat = Changed })).Raw);
        responses.Add((await ForgotAsync(client, person)).Raw);
        string token = TokenFrom(outbox.To(person).Single());
        responses.Add((await PostAnonymousAsync(client, "/api/auth/reset-password/check", new { token })).Raw);
        responses.Add((await ResetAsync(client, token, "kisa")).Raw);
        responses.Add((await ResetAsync(client, token, Reset)).Raw);
        responses.Add((await ResetAsync(client, token, Reset)).Raw);

        string log = factory.ReadLog();
        string audit = string.Join('\n', await QueryAsync(factory, c => c.AuditEntries.Select(e => $"{e.AttemptedEmail}|{e.TargetType}|{e.Origin}").ToListAsync()));
        foreach (string secret in new[] { Password, Changed, Reset, "Yanlis-Gizli-1", token })
        {
            log.ShouldNotContain(secret);
            audit.ShouldNotContain(secret);
            string.Concat(responses).ShouldNotContain(secret);
        }

        log.ShouldNotContain(person);
    }
}
