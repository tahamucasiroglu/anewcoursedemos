using Konsept.Api.Tests.Support;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Gerçek PostgreSQL 17 ile admin sorguları (spec 0010 AC-3, AC-4, AC-6; full profil): kullanıcı dizini, konsept başına ilk
/// tamamlanma (sürümler tek klip), açık bildirim sayısı, aralıktaki kullanıcılı olaylar ve sayfalı/süzülmüş aktivite listesi.
/// Veritabanı paylaşıldığı için yalnızca bu testin yazdığı benzersiz kayıtlar üzerinden assert edilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AdminStatisticsIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AdminStatisticsIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public AdminStatisticsIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Üç EF uygulaması gerçek sorgularla doğru sonuç verir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminQueries_RealDatabase_DirectoryClipsActivities()
    {
        var ct = TestContext.Current.CancellationToken;
        var tag = Guid.NewGuid().ToString("N")[..8];
        var registeredAt = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var baseTime = new DateTimeOffset(2020, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var user = User.Register("Yönetim " + tag, $"admin-int-{tag}@ornek.test", "hash", registeredAt);
        var other = User.Register("Diğer " + tag, $"diger-int-{tag}@ornek.test", "hash", registeredAt);
        var conceptA = Guid.NewGuid();
        var conceptB = Guid.NewGuid();
        int openBefore;
        await using (var before = _factory.Services.CreateAsyncScope())
        {
            openBefore = await before.ServiceProvider.GetRequiredService<IAdminActivityRepository>().CountOpenErrorReportsAsync(ct);
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            (await users.TryAddAsync(user, ct)).ShouldBeTrue();
            (await users.TryAddAsync(other, ct)).ShouldBeTrue();
            var videos = scope.ServiceProvider.GetRequiredService<IProducedVideoRepository>();
            await videos.AddAsync(Video(user.Id, conceptA, baseTime.AddDays(1)), ct);
            await videos.AddAsync(Video(user.Id, conceptA, baseTime.AddDays(2)), ct); // yeni sürüm
            await videos.AddAsync(Video(user.Id, conceptB, baseTime.AddDays(3)), ct);
            var events = scope.ServiceProvider.GetRequiredService<ISystemEventRepository>();
            for (var i = 0; i < 3; i++)
            {
                await events.AddAsync(SystemEvent.Create("identity.login_succeeded", EventLevel.Info, "olay", baseTime.AddMinutes(i), user.Id), ct);
            }

            await events.AddAsync(SystemEvent.Create("agent.connected", EventLevel.Info, "olay", baseTime.AddMinutes(10), other.Id), ct);
            await scope.ServiceProvider.GetRequiredService<IErrorReportRepository>()
                .AddAsync(ErrorReport.CreateWeb(user.Id, baseTime, "0.0.0", "bildirim " + tag, "/", null, []), ct);
            await events.AddAsync(SystemEvent.Create("system.started", EventLevel.Info, "olay", baseTime.AddMinutes(5), null), ct);
        }

        await using var read = _factory.Services.CreateAsyncScope();
        var directory = await read.ServiceProvider.GetRequiredService<IUserDirectory>().ListAsync(ct);
        var firsts = await read.ServiceProvider.GetRequiredService<IClipStatistics>().ListFirstCompletionsAsync(ct);
        var admin = read.ServiceProvider.GetRequiredService<IAdminActivityRepository>();
        var open = await admin.CountOpenErrorReportsAsync(ct);
        var times = await admin.ListUserEventTimesAsync(baseTime, baseTime.AddMinutes(11), ct);
        var page = await admin.ListActivitiesAsync([user.Id, other.Id], baseTime, baseTime.AddHours(1), 1, 2, ct);
        var onlyOther = await admin.ListActivitiesAsync([other.Id], null, null, 0, 25, ct);

        var entry = directory.Single(d => d.Id == user.Id);
        entry.Name.ShouldBe("Yönetim " + tag);
        entry.Email.ShouldBe($"admin-int-{tag}@ornek.test");
        entry.RegisteredAt.ShouldBe(registeredAt);
        firsts.Count(f => f == baseTime.AddDays(1)).ShouldBe(1);
        firsts.ShouldContain(baseTime.AddDays(3));
        firsts.ShouldNotContain(baseTime.AddDays(2)); // sürüm ayrı klip değil.
        open.ShouldBeGreaterThanOrEqualTo(openBefore + 1); // yeni "Yeni" bildirim açık sayılır (paylaşılan veritabanı: en az +1).
        times.Where(t => t.UserId == user.Id || t.UserId == other.Id).Count().ShouldBe(4);
        times.ShouldAllBe(t => t.UserId != Guid.Empty);
        page.Total.ShouldBe(4);
        page.Items.Select(e => e.OccurredAt).ShouldBe([baseTime.AddMinutes(2), baseTime.AddMinutes(1)]); // en yeni üstte, 1 atla 2 al.
        onlyOther.Total.ShouldBe(1);
        onlyOther.Items.Single().Type.ShouldBe("agent.connected");
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
