using Shouldly;
using TahaKonsept.Modules.Diagnostics.Application.Concrete;
using TahaKonsept.Modules.Diagnostics.Application.Models;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence.Repositories;
using TahaKonsept.Modules.Diagnostics.Tests.Support;
using TahaKonsept.Shared.Contracts.Diagnostics;
using Xunit;

namespace TahaKonsept.Modules.Diagnostics.Tests.ActivityLogs;

/// <summary>
/// <see cref="ActivityLogService"/>: son kayıtlar (afterId), görünürlük kapsamı, güne/seviyeye göre arama, istemci olayı yazma.
/// </summary>
public sealed class ActivityLogServiceTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 13, 20, 0, 0, TimeSpan.Zero); // 23:00 Türkiye (13 Kasım)
    private readonly FakeClock _clock = new(Start);
    private readonly DiagnosticsDbContext _db;
    private readonly Guid _me = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();

    /// <summary>Oluşturur.</summary>
    public ActivityLogServiceTests()
    {
        _db = TestDb.Create(_clock);
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _db.Dispose();

    /// <summary>afterId verilince yalnızca daha yeniler, eskiden yeniye gelir.</summary>
    [Fact]
    public async Task Recent_returns_only_newer_entries_in_ascending_order()
    {
        var ids = await SeedAsync(_me, 5);
        var service = new ActivityLogService(new ActivityLogRepository(_db), TestUsers.Regular(_me));

        var all = (await service.GetRecentAsync(null, null, TestContext.Current.CancellationToken)).Value;
        all.Select(l => l.Id).ShouldBe(ids);

        var newer = (await service.GetRecentAsync(ids[2], null, TestContext.Current.CancellationToken)).Value;
        newer.Select(l => l.Id).ShouldBe(ids.Skip(3));

        var latestTwo = (await service.GetRecentAsync(null, 2, TestContext.Current.CancellationToken)).Value;
        latestTwo.Select(l => l.Id).ShouldBe(ids.Skip(3));
    }

    /// <summary>Normal kullanıcı yalnızca kendi kayıtlarını görür (başka kullanıcı ve sistem kayıtları hariç); yönetici hepsini.</summary>
    [Fact]
    public async Task Recent_scopes_by_owner_unless_admin()
    {
        await SeedAsync(_me, 1);
        await SeedAsync(_other, 1);
        await SeedAsync(Guid.Empty, 1);

        var mine = (await new ActivityLogService(new ActivityLogRepository(_db), TestUsers.Regular(_me))
            .GetRecentAsync(null, null, TestContext.Current.CancellationToken)).Value;
        mine.Select(l => l.UserId).ShouldBe([_me]);

        var all = (await new ActivityLogService(new ActivityLogRepository(_db), TestUsers.Admin(_me))
            .GetRecentAsync(null, null, TestContext.Current.CancellationToken)).Value;
        all.Count.ShouldBe(3);
    }

    /// <summary>Arama da kapsamlıdır: B kullanıcısı A'nın ve sistemin kayıtlarını aramayla da göremez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Search_does_not_return_other_users_entries()
    {
        await AddAsync(_me, LogLevelKind.Information, "A'nın gizli işi");
        await AddAsync(Guid.Empty, LogLevelKind.Error, "sistem hatası");
        await AddAsync(_other, LogLevelKind.Information, "B'nin işi");

        var asOther = new ActivityLogService(new ActivityLogRepository(_db), TestUsers.Regular(_other));
        var page = (await asOther.SearchAsync(new ActivityLogQuery(), TestContext.Current.CancellationToken)).Value;

        page.TotalCount.ShouldBe(1);
        page.Items.ShouldHaveSingleItem().Message.ShouldBe("B'nin işi");
        (await asOther.SearchAsync(new ActivityLogQuery { Search = "gizli" }, TestContext.Current.CancellationToken)).Value.TotalCount.ShouldBe(0);
    }

    /// <summary>Başka kullanıcının kayıt kimliği çapa (afterId) olarak verilirse yok sayılır; yalnız kendi kayıtları gelir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Recent_ignores_other_users_anchor()
    {
        var mine = await SeedAsync(_other, 2);
        var foreign = await SeedAsync(_me, 1);

        var asOther = new ActivityLogService(new ActivityLogRepository(_db), TestUsers.Regular(_other));
        var result = (await asOther.GetRecentAsync(foreign[0], null, TestContext.Current.CancellationToken)).Value;

        result.Select(l => l.Id).ShouldBe(mine);
    }

    /// <summary>Gün filtresi Türkiye saatine göre çalışır; seviye filtresi uygulanır; en yeni önce.</summary>
    [Fact]
    public async Task Search_filters_by_local_day_and_level()
    {
        await AddAsync(_me, LogLevelKind.Information, "13 Kasım 23:00"); // 20:00Z
        _clock.Advance(TimeSpan.FromHours(2)); // 22:00Z = 14 Kasım 01:00 Türkiye
        await AddAsync(_me, LogLevelKind.Error, "14 Kasım 01:00 hata");
        _clock.Advance(TimeSpan.FromMinutes(5));
        await AddAsync(_me, LogLevelKind.Information, "14 Kasım 01:05");

        var service = new ActivityLogService(new ActivityLogRepository(_db), TestUsers.Admin(_me));

        var day14 = (await service.SearchAsync(new ActivityLogQuery { Day = new DateOnly(2026, 11, 14) }, TestContext.Current.CancellationToken)).Value;
        day14.TotalCount.ShouldBe(2);
        day14.Items[0].Message.ShouldBe("14 Kasım 01:05");

        var errors = (await service.SearchAsync(new ActivityLogQuery { Day = new DateOnly(2026, 11, 14), Level = LogLevelKind.Error }, TestContext.Current.CancellationToken)).Value;
        errors.Items.ShouldHaveSingleItem().Message.ShouldBe("14 Kasım 01:00 hata");

        var day13 = (await service.SearchAsync(new ActivityLogQuery { Day = new DateOnly(2026, 11, 13) }, TestContext.Current.CancellationToken)).Value;
        day13.Items.ShouldHaveSingleItem().Message.ShouldBe("13 Kasım 23:00");
    }

    /// <summary>İstemci olayı oturum kullanıcısı adına, boş kaynak "Web" olarak yazılır.</summary>
    [Fact]
    public async Task Create_writes_client_event_for_current_user()
    {
        var service = new ActivityLogService(new ActivityLogRepository(_db), TestUsers.Regular(_me));

        var result = await service.CreateAsync(new ActivityLogCreateRequest { Level = LogLevelKind.Warning, Message = "Önizleme açılamadı" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe(ActivitySources.Web);
        result.Value.UserId.ShouldBe(_me);
        _db.ActivityLogs.Count().ShouldBe(1);
    }

    private async Task<List<Guid>> SeedAsync(Guid owner, int count)
    {
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            ids.Add(await AddAsync(owner, LogLevelKind.Information, $"kayıt {i}"));
            _clock.Advance(TimeSpan.FromSeconds(1));
        }

        return ids;
    }

    private async Task<Guid> AddAsync(Guid owner, LogLevelKind level, string message)
    {
        var log = ActivityLog.Create(owner, level, "Test", message);
        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return log.Id;
    }
}
