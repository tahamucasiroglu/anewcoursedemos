using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Features.Billing.Reminders;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Infrastructure.Billing;
using OkulYonetim.Persistence.Repositories;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-10 / plan Risks 2 (arka plan işçisinin okul turu — 0015 review R1-2): <see cref="OverdueReminderWorker.RunOnceAsync"/>
/// doğrudan çağrılır; işçi bütün etkin okulları dolaştığı için test kendi boş veritabanında üç okulla koşar (paylaşılan test veritabanındaki
/// başka testlerin okullarına hatırlatma göndermesin). Etkin okul listesinde (<c>ListActiveTenantIdsAsync</c>; pasif okulu vermez) önde olan B okulunun çalışması hata verir (yalnızca o okul için
/// hata atan bağımlılık) → okul kimliğiyle tek Error log'u, A yine işlenir (Ali'nin iki velisine birer hatırlatma); pasif C okulunun vadesi
/// geçmiş taksidi olsa da hiçbir şey olmaz. Her etkin okul kendi DI kapsamında ve kendi okuluyla sınırlı sistem oturumunda çalışır; A'nın
/// bildirimleri yalnızca A'nın velilerine gider. Çalıştırıcı (<see cref="OverdueReminderRunner"/>) yalnızca okulu henüz seçilmemiş taze
/// bir kapsamda açılır: aynı kapsamda ikinci çalışma (aynı ya da başka okul) reddedilir (0015 review R1-4).
/// </summary>
[Trait("Category", "Integration")]
public sealed class OverdueReminderWorkerTests
{
    private readonly TestDatabase database;

    public OverdueReminderWorkerTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Runner_ScopeWithSchoolAlreadySet_Rejected()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using IServiceScope scope = factory.Services.CreateScope();
        OverdueReminderRunner runner = scope.ServiceProvider.GetRequiredService<OverdueReminderRunner>();
        Guid tenantId = Guid.CreateVersion7();

        (await runner.RunAsync(tenantId, TestContext.Current.CancellationToken)).ShouldBe(0);

        await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(tenantId, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunOnce_FailingSchoolLoggedOthersContinue_InactiveSkipped_EachSchoolOwnScope()
    {
        TestDatabase own = new();
        await TestDatabase.CreateAsync(own.Name);
        try
        {
            FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
            CapturingPushNotifier pushes = new();
            ConcurrentQueue<(Guid? TenantId, bool Authenticated, IServiceProvider Scope)> resolutions = new();
            Guid? failingTenant = null;
            using OkulApiFactory factory = new(own.ConnectionString, timeProvider: time, configureServices: services =>
            {
                services.RemoveAll<IEmailOutbox>();
                services.AddSingleton<IEmailOutbox>(new Account.CapturingOutbox());
                services.RemoveAll<IPushNotifier>();
                services.AddSingleton<IPushNotifier>(pushes);
                services.RemoveAll<IBillingRepository>();
                services.AddScoped<BillingRepository>();
                services.AddScoped<IBillingRepository>(sp =>
                {
                    ICurrentSession session = sp.GetRequiredService<ICurrentSession>();
                    resolutions.Enqueue((session.TenantId, session.IsAuthenticated, sp));
                    return failingTenant is { } failing && session.TenantId == failing
                        ? throw new InvalidOperationException("test: bu okulun deposu hata veriyor")
                        : sp.GetRequiredService<BillingRepository>();
                });
            });
            using HttpClient client = factory.CreateClient();

            BillingWorld first = await BillingWorldAsync(factory, client);
            await PlanAsync(client, first.Founder, PlanBody(first.Ali, first.FeeId, installmentCount: 1, firstDueDate: Today));
            BillingWorld second = await BillingWorldAsync(factory, client);
            await PlanAsync(client, second.Founder, PlanBody(second.Ali, second.FeeId, installmentCount: 1, firstDueDate: Today));
            BillingWorld c = await BillingWorldAsync(factory, client);
            await PlanAsync(client, c.Founder, PlanBody(c.Ali, c.FeeId, installmentCount: 1, firstDueDate: Today));
            await QueryAsync(factory, db => db.Tenants.IgnoreQueryFilters().Where(t => t.Id == c.TenantId).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false)));

            // Okul kimlikleri sahte saatle aynı anda üretildiği için işçinin sırası açılış sırası değildir: hata verecek okul (B),
            // işçinin kullandığı listede önde olan seçilir — böylece hata, sonraki okul (A) işlenmeden önce gelir.
            IReadOnlyList<Guid> order;
            using (IServiceScope listScope = factory.Services.CreateScope())
            {
                order = await listScope.ServiceProvider.GetRequiredService<IPlatformDirectory>().ListActiveTenantIdsAsync(TestContext.Current.CancellationToken);
            }

            order.ShouldBe([first.TenantId, second.TenantId], ignoreOrder: true, "yalnızca etkin okullar");
            (BillingWorld b, BillingWorld a) = order[0] == first.TenantId ? (first, second) : (second, first);
            failingTenant = b.TenantId;
            resolutions.Clear();
            time.SetUtcNow(new DateTimeOffset(2026, 11, 21, 8, 0, 0, TimeSpan.Zero));
            OverdueReminderWorker worker = new(
                factory.Services.GetRequiredService<IServiceScopeFactory>(),
                time,
                factory.Services.GetRequiredService<ILogger<OverdueReminderWorker>>());

            int sent = await worker.RunOnceAsync(TestContext.Current.CancellationToken);

            sent.ShouldBe(1, "yalnızca A'nın taksidi");
            resolutions.Select(r => r.TenantId).ShouldBe([b.TenantId, a.TenantId], "B sırada önde, hata vermesine rağmen A işlendi; pasif C hiç açılmadı");
            resolutions.ShouldAllBe(r => !r.Authenticated, "sistem oturumu: kişi yok");
            resolutions.Select(r => r.Scope).Distinct().Count().ShouldBe(2, "her okul kendi DI kapsamında");
            pushes.Sent.Select(p => p.RecipientUserId).ShouldBe([a.School.AliMother.UserId, a.School.AliFather.UserId], ignoreOrder: true);
            (await ReminderNoticeCountAsync(factory, a.School.AliMother.UserId)).ShouldBe(1);
            (await ReminderNoticeCountAsync(factory, b.School.AliMother.UserId)).ShouldBe(0, "hata veren okul");
            (await ReminderNoticeCountAsync(factory, c.School.AliMother.UserId)).ShouldBe(0, "pasif okul");
            (await QueryAsync(factory, db => db.Set<PaymentReminder>().IgnoreQueryFilters().Select(r => r.TenantId).ToListAsync())).ShouldBe([a.TenantId]);

            string[] errors = [.. factory.ReadLog().Split('\n').Where(l => l.Contains("Gecikme hatırlatması çalışması hata verdi", StringComparison.Ordinal))];
            errors.Length.ShouldBe(1);
            errors[0].ShouldContain("[ERR]");
            errors[0].ShouldContain(b.TenantId.ToString());
        }
        finally
        {
            await own.DisposeAsync();
        }
    }
}
