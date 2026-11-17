using Konsept.Api.Tests.Support;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Gerçek PostgreSQL 17 ile ban sorguları (spec 0024 AC-2, AC-4, AC-5, AC-6, AC-10, AC-12; full profil): migration
/// <c>AddUserBans</c>, eşzamanlı iki bandan yalnızca birinin satırı değiştirmesi, banlı kullanıcının oturumunun ve ajan cihazının
/// bulunmaması, dizinde banlı bayrağı, ban kayıtları ve kullanıcı başına klip sayısı. Paylaşılan veritabanı: yalnızca bu testin
/// kayıtları üzerinden assert edilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AdminUsersIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AdminUsersIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public AdminUsersIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Ban/ban kaldırma gerçek SQL ile atomik ve her istekteki kontroller banlıyı dışlar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ban_RealDatabase_AtomicAndExcludedFromSessionAndAgent()
    {
        var ct = TestContext.Current.CancellationToken;
        var tag = Guid.NewGuid().ToString("N")[..8];
        var now = new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);
        var user = User.Register("Ban " + tag, $"ban-int-{tag}@ornek.test", "hash", now);
        var admin = User.Register("Admin " + tag, $"ban-admin-{tag}@ornek.test", "hash", now, Role.Admin);
        var session = UserSession.Open(user.Id, "hash-" + tag, DateTimeOffset.UtcNow);
        var device = AgentDevice.Pair(user.Id, "INT-PC", "0.5.0", ("dev-" + tag).PadRight(64, '0'), DateTimeOffset.UtcNow);
        var concept = Guid.NewGuid();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            (await users.TryAddAsync(user, ct)).ShouldBeTrue();
            (await users.TryAddAsync(admin, ct)).ShouldBeTrue();
            await scope.ServiceProvider.GetRequiredService<ISessionRepository>().AddAsync(session, ct);
            await scope.ServiceProvider.GetRequiredService<IAgentDeviceRepository>().AddAsync(device, ct);
            var videos = scope.ServiceProvider.GetRequiredService<IProducedVideoRepository>();
            await videos.AddAsync(Video(user.Id, concept, now), ct);
            await videos.AddAsync(Video(user.Id, concept, now.AddDays(1)), ct); // yeni sürüm: ayrı klip değil.
            await videos.AddAsync(Video(user.Id, Guid.NewGuid(), now.AddDays(2)), ct);
        }

        await using (var before = _factory.Services.CreateAsyncScope())
        {
            (await before.ServiceProvider.GetRequiredService<ISessionRepository>().FindWithRoleAsync(session.Id, ct)).ShouldNotBeNull();
            (await before.ServiceProvider.GetRequiredService<IAgentDeviceRepository>().FindByTokenHashAsync(device.TokenHash, ct)).ShouldNotBeNull();
        }

        // Eşzamanlı iki ban (ayrı kapsamlar = ayrı bağlantılar): yalnızca biri satırı değiştirir.
        async Task<bool> BanAsync()
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IUserRepository>().SetBannedAsync(user.Id, now, ct);
        }

        var results = await Task.WhenAll(BanAsync(), BanAsync());
        results.Count(r => r).ShouldBe(1);

        await using (var read = _factory.Services.CreateAsyncScope())
        {
            var sp = read.ServiceProvider;
            (await sp.GetRequiredService<ISessionRepository>().FindWithRoleAsync(session.Id, ct)).ShouldBeNull();
            (await sp.GetRequiredService<IAgentDeviceRepository>().FindByTokenHashAsync(device.TokenHash, ct)).ShouldBeNull();
            (await sp.GetRequiredService<IUserDirectory>().ListAsync(ct)).Single(d => d.Id == user.Id).IsBanned.ShouldBeTrue();
            (await sp.GetRequiredService<IUserRepository>().ListAsync(ct)).Single(u => u.Id == user.Id).BannedAt.ShouldBe(now);
            (await sp.GetRequiredService<IClipStatistics>().CountByUserAsync(ct))[user.Id].ShouldBe(2);
            var bans = sp.GetRequiredService<IUserBanRepository>();
            await bans.AddAsync(UserBanRecord.Create(user.Id, BanAction.Ban, admin.Id, now, new string('s', 500)), ct);
            await bans.AddAsync(UserBanRecord.Create(user.Id, BanAction.Unban, admin.Id, now.AddMinutes(1), "yok sayılır"), ct);
            var records = await bans.ListForUsersAsync([user.Id], ct);
            records.Select(r => r.Action).ShouldBe([BanAction.Unban, BanAction.Ban]);
            records[0].Reason.ShouldBeNull();
            records[1].Reason!.Length.ShouldBe(500);
            records.ShouldAllBe(r => r.AdminId == admin.Id);
            (await sp.GetRequiredService<IUserRepository>().SetBannedAsync(user.Id, null, ct)).ShouldBeTrue();
            (await sp.GetRequiredService<IUserRepository>().SetBannedAsync(user.Id, null, ct)).ShouldBeFalse();
        }

        await using (var after = _factory.Services.CreateAsyncScope())
        {
            (await after.ServiceProvider.GetRequiredService<IUserDirectory>().ListAsync(ct)).Single(d => d.Id == user.Id).IsBanned.ShouldBeFalse();
            (await after.ServiceProvider.GetRequiredService<ISessionRepository>().FindWithRoleAsync(session.Id, ct)).ShouldNotBeNull(); // oturum iptali ayrı adımdır (servis).
        }
    }

    private static ProducedVideo Video(Guid userId, Guid conceptId, DateTimeOffset endedAt) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        ProfileId = Guid.NewGuid(),
        ConceptId = conceptId,
        JobId = Guid.NewGuid(),
        Title = "t",
        ConceptText = "{}",
        Engine = "remotion",
        FileName = "v.mp4",
        StartedAt = endedAt.AddMinutes(-5),
        EndedAt = endedAt,
    };
}
