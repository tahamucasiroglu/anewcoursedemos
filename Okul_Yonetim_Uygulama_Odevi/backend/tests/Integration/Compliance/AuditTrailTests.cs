using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Compliance;

/// <summary>Audit kaydı (spec 0004 AC-12, S6): her giriş olayı ve okul açma kayda geçer; kayıt değiştirilemez ve silinemez.</summary>
[Trait("Category", "Integration")]
public sealed class AuditTrailTests
{
    private const string ClientIp = "203.0.113.7";
    private readonly TestDatabase database;

    public AuditTrailTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task LoginEvents_AreRecordedWithWhoWhatWhenAndForwardedIp()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ClientIp);
        SchoolSeed first = await CreateSchoolAsync(factory);
        SchoolSeed second = await CreateSchoolAsync(factory, email: first.FounderEmail);
        string email = first.FounderEmail;

        for (int i = 0; i < 3; i++)
        {
            using HttpResponseMessage wrong = await PostLoginAsync(client, email, "Yanlis-Sifre-7");
        }

        await IdentityTestData.WithContextAsync(factory, async c =>
            await c.LoginThrottles.Where(t => t.Email == email).ExecuteDeleteAsync());
        JsonElement auth = await LoginAsync(client, email);
        using (HttpResponseMessage select = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/auth/select-tenant", AccessToken(auth), new { tenantId = second.TenantId }))
        {
            auth = await ReadJsonAsync(select);
        }

        using (HttpResponseMessage logout = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/auth/logout", AccessToken(auth)))
        {
            logout.EnsureSuccessStatusCode();
        }

        List<AuditEntry> entries = await QueryAsync(factory, c => c.AuditEntries
            .Where(a => a.AttemptedEmail == email || a.ActorUserId == first.FounderId).OrderBy(a => a.OccurredAt).ThenBy(a => a.Id).ToListAsync());
        entries.Select(e => e.Action).ShouldBe(
        [
            AuditAction.LoginFailed, AuditAction.LoginFailed, AuditAction.LoginFailed, AuditAction.LoginLockedOut,
            AuditAction.LoginSucceeded, AuditAction.TenantSelected, AuditAction.Logout,
        ], ignoreOrder: true);
        entries.ShouldAllBe(e => e.IpAddress == ClientIp);
        entries.Where(e => e.Action is AuditAction.LoginSucceeded or AuditAction.TenantSelected or AuditAction.Logout)
            .ShouldAllBe(e => e.ActorUserId == first.FounderId && e.TargetId != null);
        entries.Single(e => e.Action == AuditAction.TenantSelected).TenantId.ShouldBe(second.TenantId);
        entries.Where(e => e.Action == AuditAction.LoginFailed).ShouldAllBe(e => e.AttemptedEmail == email);
    }

    [Fact]
    public async Task CreateSchool_RecordsTenantBranchUserAndRoleEvents()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        SchoolSeed school = await CreateSchoolAsync(factory);

        List<AuditAction> actions = await QueryAsync(factory, c => c.AuditEntries
            .Where(a => a.TenantId == school.TenantId).Select(a => a.Action).ToListAsync());

        actions.ShouldBe([AuditAction.TenantCreated, AuditAction.BranchCreated, AuditAction.UserCreated, AuditAction.RoleAssigned], ignoreOrder: true);
    }

    [Theory]
    [InlineData("UPDATE audit_entries SET ip_address = 'degisti' WHERE id = @id")]
    [InlineData("DELETE FROM audit_entries WHERE id = @id")]
    [InlineData("TRUNCATE audit_entries")]
    public async Task AuditEntries_CannotBeChangedOrDeleted(string sql)
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        SchoolSeed school = await CreateSchoolAsync(factory);
        Guid id = await QueryAsync(factory, c => c.AuditEntries.Where(a => a.TenantId == school.TenantId).Select(a => a.Id).FirstAsync());

        await using NpgsqlConnection connection = new(this.database.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("id", id);

        PostgresException error = await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
        error.MessageText.ShouldContain("yalnızca eklenebilir");
        (await QueryAsync(factory, c => c.AuditEntries.CountAsync(a => a.Id == id))).ShouldBe(1);
    }
}
