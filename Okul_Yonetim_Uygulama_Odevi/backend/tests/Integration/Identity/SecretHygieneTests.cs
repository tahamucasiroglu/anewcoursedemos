using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>
/// Şifre ve anahtar hijyeni (spec 0004 AC-11, S3): şifre veritabanında yalnızca özet; giriş, hatalı giriş, yenileme,
/// çıkış ve doğrulama hatası akışlarının log, yanıt ve audit kayıtlarında şifre ve anahtar yok.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SecretHygieneTests
{
    private readonly TestDatabase database;

    public SecretHygieneTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task AuthFlows_NeverExposePasswordOrTokens_InLogsResponsesOrAudit()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        const string password = "Hijyen-Sifresi-4242";
        const string wrongPassword = "Yanlis-Hijyen-5151";
        SchoolSeed school = await CreateSchoolAsync(factory, password: password);
        List<string> responses = [];

        using (HttpResponseMessage wrong = await PostLoginAsync(client, school.FounderEmail, wrongPassword))
        {
            responses.Add(await wrong.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        using (HttpResponseMessage invalid = await PostLoginAsync(client, "gecersiz", password))
        {
            responses.Add(await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        JsonElement auth = await LoginAsync(client, school.FounderEmail, password);
        string access = AccessToken(auth);
        string refresh = auth.GetProperty("refreshToken").GetString()!;
        using (HttpResponseMessage refreshed = await client.PostAsJsonAsync(new Uri("/api/auth/refresh", UriKind.Relative), new { refreshToken = refresh }, TestContext.Current.CancellationToken))
        {
            refreshed.EnsureSuccessStatusCode();
        }

        using (HttpResponseMessage logout = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/auth/logout", access))
        {
            logout.EnsureSuccessStatusCode();
        }

        string log = factory.ReadLog();
        string audit = string.Join('\n', await QueryAsync(factory, c => c.AuditEntries
            .Where(a => a.AttemptedEmail == school.FounderEmail || a.ActorUserId == school.FounderId || a.TenantId == school.TenantId)
            .Select(a => (a.AttemptedEmail ?? string.Empty) + "|" + (a.TargetType ?? string.Empty) + "|" + (a.IpAddress ?? string.Empty))
            .ToListAsync()));
        string storedHash = await QueryAsync(factory, c => c.Users.Where(u => u.Id == school.FounderId).Select(u => u.PasswordHash).SingleAsync());
        string storedRefresh = string.Join('\n', await QueryAsync(factory, c => c.UserSessions.Where(s => s.UserId == school.FounderId).Select(s => s.RefreshTokenHash).ToListAsync()));

        foreach (string secret in new[] { password, wrongPassword, refresh, access })
        {
            log.ShouldNotContain(secret);
            audit.ShouldNotContain(secret);
            responses.ShouldAllBe(r => !r.Contains(secret, StringComparison.Ordinal));
            storedRefresh.ShouldNotContain(secret);
        }

        storedHash.ShouldNotContain(password);
        storedHash.Length.ShouldBeGreaterThan(40);
        log.ShouldNotContain(school.FounderEmail);
    }
}
