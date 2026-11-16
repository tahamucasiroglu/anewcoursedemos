using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Identity.Application;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Modules.Identity.Infrastructure.Persistence;
using Konsept.Modules.Identity.Infrastructure.Persistence.Concrete;
using Konsept.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Kimlik, gerçek PostgreSQL 17 ile (full profil): atomik kilit sayacı (AC-6, AC-7), tekil e-posta ve eşzamanlı kayıt
/// (AC-2, AC-16), kayıt zamanı (AC-1), tüm oturumları iptal (AC-14), şifre hash'i (AC-17) ve olay süzgeci (AC-15).
/// </summary>
[Trait("Category", "Integration")]
public sealed class IdentityIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="IdentityIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public IdentityIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Kayıt zamanı UTC olarak veritabanına yazılır; şifre hash'i ASP.NET Core Identity V3 biçimindedir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_PersistsRegisteredAtUtc_PasswordHashNotPlainV3Format()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = AuthenticatedClient.NewEmail();
        var before = DateTimeOffset.UtcNow.AddSeconds(-5);

        await AuthenticatedClient.RegisterAsync(client, _factory.Services, email, AuthenticatedClient.Password, "Kalıcı", ct);

        var registeredAt = (DateTime)(await ScalarAsync($"SELECT registered_at FROM identity.users WHERE email = '{email}'"))!;
        var hash = (string)(await ScalarAsync($"SELECT password_hash FROM identity.users WHERE email = '{email}'"))!;
        new DateTimeOffset(registeredAt, TimeSpan.Zero).ShouldBeGreaterThan(before);
        hash.ShouldNotContain(AuthenticatedClient.Password);
        Convert.FromBase64String(hash)[0].ShouldBe((byte)0x01);
        (await CountAsync($"SELECT count(*) FROM identity.users WHERE password_hash = '{AuthenticatedClient.Password}'")).ShouldBe(0);
    }

    /// <summary>Normalleştirilmiş e-posta tekil indeksi veritabanında zorlanır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task NormalizedEmail_UniqueIndexEnforced()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = AuthenticatedClient.NewEmail();
        await AuthenticatedClient.RegisterAsync(client, _factory.Services, email, AuthenticatedClient.Password, "Tekil", ct);

        var duplicate = await Should.ThrowAsync<PostgresException>(() => ScalarAsync(
            "INSERT INTO identity.users (id, name, email, normalized_email, password_hash, failed_login_count, registered_at) " +
            $"VALUES (gen_random_uuid(), 'x', 'x', '{email.ToUpperInvariant()}', 'h', 0, now())"));

        duplicate.SqlState.ShouldBe("23505");
    }

    /// <summary>Aynı e-postayla eşzamanlı 5 kayıttan yalnızca biri hesap açar; diğerleri "zaten kayıtlı".</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_ParallelSameEmail_OneAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = AuthenticatedClient.NewEmail();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => AuthenticatedClient.PostAsync(
            client, _factory.Services, "/api/auth/register", new { name = "Çift", email, password = AuthenticatedClient.Password }, ct)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(4);
        (await CountAsync($"SELECT count(*) FROM identity.users WHERE normalized_email = '{email.ToUpperInvariant()}'")).ShouldBe(1);
        Array.ForEach(responses, r => r.Dispose());
    }

    /// <summary>
    /// Aynı hesaba eşzamanlı 10 yanlış deneme: tam olarak 2'si "kalan hak" alır (2 ve 1), gerisi kilit; sayaç 3'ten fazla
    /// hak vermez, hesap kilitlenir ve sonraki denemeler açılma zamanını uzatmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Lockout_ConcurrentWrongAttempts_NeverMoreThanThree_LockedUntilNotExtended()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = AuthenticatedClient.NewEmail();
        await AuthenticatedClient.RegisterAsync(client, _factory.Services, email, AuthenticatedClient.Password, "Eşzamanlı", ct);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => AuthenticatedClient.PostAsync(
            client, _factory.Services, "/api/auth/login", new { email, password = "Yanlis123" }, ct)));
        var problems = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<JsonElement>(ct)));
        var lockedUntil = await ScalarAsync($"SELECT locked_until FROM identity.users WHERE email = '{email}'");
        using var later = await AuthenticatedClient.PostAsync(client, _factory.Services, "/api/auth/login", new { email, password = "Yanlis123" }, ct);
        using var correct = await AuthenticatedClient.PostAsync(client, _factory.Services, "/api/auth/login", new { email, password = AuthenticatedClient.Password }, ct);

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Unauthorized);
        problems.Where(p => p.GetProperty("code").GetString() == IdentityErrors.WrongPasswordCode)
            .Select(p => p.GetProperty("remainingAttempts").GetInt32()).Order().ShouldBe([1, 2]);
        problems.Count(p => p.GetProperty("code").GetString() == IdentityErrors.LockedCode).ShouldBe(8);
        lockedUntil.ShouldNotBeNull();
        (await ScalarAsync($"SELECT locked_until FROM identity.users WHERE email = '{email}'")).ShouldBe(lockedUntil);
        (await CountAsync($"SELECT failed_login_count FROM identity.users WHERE email = '{email}'")).ShouldBe(0);
        correct.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        Array.ForEach(responses, r => r.Dispose());
    }

    /// <summary>Çıkış, kullanıcının bütün oturumlarını veritabanında iptal eder; diğer oturumun token'ı reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task RevokeAll_RealDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var email = AuthenticatedClient.NewEmail();
        var registered = await AuthenticatedClient.RegisterAsync(client, _factory.Services, email, AuthenticatedClient.Password, "Çıkış", ct);
        var other = await AuthenticatedClient.LoginAsync(client, _factory.Services, email, AuthenticatedClient.Password, ct);
        var userId = registered.GetProperty("userId").GetGuid();

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.GetProperty("accessToken").GetString());
        (await client.SendAsync(logout, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        using var session = new HttpRequestMessage(HttpMethod.Get, "/api/auth/session");
        session.Headers.Authorization = new AuthenticationHeaderValue("Bearer", other.GetProperty("accessToken").GetString());
        using var sessionResponse = await client.SendAsync(session, ct);

        sessionResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await CountAsync($"SELECT count(*) FROM identity.sessions WHERE user_id = '{userId}' AND revoked_at IS NOT NULL")).ShouldBe(2);
        (await CountAsync($"SELECT count(*) FROM identity.sessions WHERE user_id = '{userId}' AND revoked_at IS NULL")).ShouldBe(0);
    }

    /// <summary>Günlük şeridi kullanıcıya göre süzülür: B, A'nın daha yeni olaylarını görmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task LatestVisibleEvent_FiltersByUser()
    {
        var ct = TestContext.Current.CancellationToken;
        using var clientB = await AuthenticatedClient.CreateAsync(_factory, ct, name: "B");
        using var clientA = await AuthenticatedClient.CreateAsync(_factory, ct, name: "A");
        var userA = (await clientA.GetFromJsonAsync<JsonElement>("/api/auth/session", ct)).GetProperty("userId").GetGuid();

        var statusB = await clientB.GetFromJsonAsync<JsonElement>("/api/system/status", ct);
        var statusA = await clientA.GetFromJsonAsync<JsonElement>("/api/system/status", ct);

        statusB.GetProperty("lastEvent").GetProperty("userId").GetGuid().ShouldNotBe(userA);
        statusA.GetProperty("lastEvent").GetProperty("userId").GetGuid().ShouldBe(userA);
        (await CountAsync($"SELECT count(*) FROM monitoring.system_events WHERE \"UserId\" = '{userA}'")).ShouldBe(2);
    }

    /// <summary>
    /// <c>AddUserRole</c> migration'ı mevcut satırları <c>user</c> yapar (spec 0011 AC-1): migration öncesi eklenen hesap
    /// sonrasında kullanıcı rolündedir; sütun yalnızca <c>user</c>/<c>admin</c> kabul eder.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Migration_AddUserRole_ExistingRowsUser()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = new PostgresTestDatabase();
        await database.InitializeAsync();
        await using var context = CreateContext(database);
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync("20261114183027_InitialIdentity", ct);
        await database.ScalarAsync(
            "INSERT INTO identity.users (id, name, email, normalized_email, password_hash, failed_login_count, registered_at) " +
            "VALUES (gen_random_uuid(), 'Eski', 'eski@ornek.test', 'ESKI@ORNEK.TEST', 'h', 0, now())");
        await migrator.MigrateAsync(cancellationToken: ct);

        (await database.ScalarAsync("SELECT role FROM identity.users WHERE normalized_email = 'ESKI@ORNEK.TEST'")).ShouldBe("user");
        var invalid = await Should.ThrowAsync<PostgresException>(() => database.ScalarAsync("UPDATE identity.users SET role = 'root'"));
        invalid.SqlState.ShouldBe("23514");
    }

    /// <summary>
    /// Gerçek veritabanında açılış senkronu (spec 0011 AC-4): ayardaki e-postanın hesabı admin olur, eski admin kullanıcıya
    /// döner, ayar boşken (<c>null</c>) kimse admin kalmaz; oturum doğrulama sorgusu rolü aynı sorguda döndürür.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminRoleSync_RealDatabase_PromotesAndDemotes()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = new PostgresTestDatabase();
        await database.InitializeAsync();
        await using var context = CreateContext(database);
        await context.Database.MigrateAsync(ct);
        var users = new UserRepository(context);
        var sessions = new SessionRepository(context);
        var now = DateTimeOffset.UtcNow;
        var target = User.Register("Hedef", "Admin@Ornek.Test", "h", now);
        var former = User.Register("Eski", "eski@ornek.test", "h", now, Role.Admin);
        var plain = User.Register("Düz", "duz@ornek.test", "h", now);
        foreach (var user in new[] { target, former, plain })
        {
            (await users.TryAddAsync(user, ct)).ShouldBeTrue();
        }

        var session = UserSession.Open(target.Id, "hash-0011", now);
        await sessions.AddAsync(session, ct);

        var changed = await users.SyncAdminRoleAsync(AdminEmailPolicy.NormalizedAdminEmail("admin@ornek.test"), ct);
        var again = await users.SyncAdminRoleAsync(AdminEmailPolicy.NormalizedAdminEmail("admin@ornek.test"), ct);
        var withRole = await sessions.FindWithRoleAsync(session.Id, ct);
        var roles = await RolesAsync(database);
        var cleared = await users.SyncAdminRoleAsync(null, ct);
        var rolesAfterClear = await RolesAsync(database);

        changed.ShouldBe(2);
        again.ShouldBe(0);
        withRole!.Value.Role.ShouldBe(Role.Admin);
        withRole.Value.Session.Id.ShouldBe(session.Id);
        roles.ShouldBe("ADMIN@ORNEK.TEST=admin,DUZ@ORNEK.TEST=user,ESKI@ORNEK.TEST=user");
        cleared.ShouldBe(1);
        rolesAfterClear.ShouldBe("ADMIN@ORNEK.TEST=user,DUZ@ORNEK.TEST=user,ESKI@ORNEK.TEST=user");
        (await sessions.FindWithRoleAsync(Guid.NewGuid(), ct)).ShouldBeNull();
    }

    /// <summary>
    /// Ajan cihazı (spec 0012, migration <c>AddAgentDevices</c>) gerçek veritabanında: eşleştirme cihazı yazar, belirteç yalnızca
    /// hash olarak durur, son kullanım kaydırılır, çıkış cihazı iptal eder.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentDevices_RealDatabase_RevokeAndTouch()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var user = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        using var agent = _factory.CreateClient();
        var machine = "DB-PC-" + Guid.NewGuid().ToString("N")[..6];

        var token = await AgentPairing.PairAsync(user, agent, _factory.Services, ct, machine);

        var userId = (Guid)(await ScalarAsync($"SELECT user_id FROM identity.agent_devices WHERE machine_name = '{machine}'"))!;
        (await CountAsync($"SELECT count(*) FROM identity.agent_devices WHERE user_id = '{userId}' AND revoked_at IS NULL")).ShouldBe(1);
        (await CountAsync($"SELECT count(*) FROM identity.agent_devices WHERE token_hash = '{token}'")).ShouldBe(0);

        await using (var context = CreateContext(_factory.Database))
        {
            var devices = new AgentDeviceRepository(context);
            var device = await devices.FindByTokenHashAsync(new Konsept.Modules.Identity.Application.Concrete.RefreshTokenFactory().Hash(token), ct);
            device.ShouldNotBeNull();
            await devices.TouchAsync(device.Id, device.LastUsedAt.AddDays(30), ct);
            (await CountAsync($"SELECT count(*) FROM identity.agent_devices WHERE id = '{device.Id}' AND last_used_at > created_at + interval '29 days'")).ShouldBe(1);
        }

        using var logout = await user.PostAsync(new Uri("/api/auth/logout", UriKind.Relative), null, ct);
        logout.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await CountAsync($"SELECT count(*) FROM identity.agent_devices WHERE user_id = '{userId}' AND revoked_at IS NULL")).ShouldBe(0);
    }

    private static IdentityModuleDbContext CreateContext(PostgresTestDatabase database) =>
        new(new DbContextOptionsBuilder<IdentityModuleDbContext>()
            .UseNpgsql(database.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", IdentityModuleDbContext.SchemaName))
            .Options);

    private static async Task<string?> RolesAsync(PostgresTestDatabase database) =>
        (string?)await database.ScalarAsync("SELECT string_agg(normalized_email || '=' || role, ',' ORDER BY normalized_email) FROM identity.users");

    private Task<object?> ScalarAsync(string sql) => _factory.Database.ScalarAsync(sql);

    private async Task<long> CountAsync(string sql) =>
        Convert.ToInt64(await _factory.Database.ScalarAsync(sql), CultureInfo.InvariantCulture);
}
