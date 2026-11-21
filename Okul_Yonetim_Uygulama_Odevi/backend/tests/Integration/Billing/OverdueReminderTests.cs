using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-10 (otomatik hatırlatma; okul çalışması işçinin yaptığı gibi yeni kapsam + sistem oturumuyla): vade+1. gün 10:00'dan
/// (Türkiye saati) önce hiçbir şey gitmez, sonra öğrencinin okulda etkin her velisine bir bildirim gider ve velinin akışında "Okul
/// bildirimi" olarak görünür (metin spec'teki gibi); vade+8 ve vade+15'te tekrar, 4. hiç gitmez; birkaç gün kapalı kalan sistem açılınca
/// yalnızca bir hatırlatma; ödenen taksit, iptal plan, ayrılmış öğrenci, pasif veli ve başka öğrencinin velisi bildirim almaz; aynı anda
/// iki çalışma tek bildirim üretir; bildirim kapısı hata verirse Warning loglanır ve diğer hatırlatmalar sürer.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OverdueReminderTests
{
    private readonly TestDatabase database;

    public OverdueReminderTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FirstAtTenNextDay_ThenDayEightAndFifteen_NeverFourth_ShownInGuardianFeed()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        CapturingPushNotifier pushes = new();
        using OkulApiFactory factory = BillingFactory(this.database, pushes, time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 1, firstDueDate: Today));

        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(0, "vade günü");
        time.SetUtcNow(new DateTimeOffset(2026, 11, 21, 6, 59, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(0, "Türkiye 09:59");
        pushes.Sent.ShouldBeEmpty();

        time.SetUtcNow(new DateTimeOffset(2026, 11, 21, 7, 0, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(1);
        pushes.Sent.Select(p => p.RecipientUserId).ShouldBe([w.School.AliMother.UserId, w.School.AliFather.UserId], ignoreOrder: true);
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(0, "aynı gün ikinci kez gitmez");

        string mother = await CommunicationTestData.ReloginAsync(client, w.School.Class, w.School.AliMother);
        JsonElement item = (await CommunicationTestData.FeedAsync(client, mother)).Body.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("kind").GetString().ShouldBe("SchoolNotice");
        item.GetProperty("title").GetString().ShouldBe("Ali Yılmaz için 20.11.2026 vadeli taksidin 10.000,00 TL'si ödenmemiştir. Bilgi için okul muhasebesiyle görüşebilirsiniz.");

        time.SetUtcNow(new DateTimeOffset(2026, 11, 28, 7, 30, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(1);
        time.SetUtcNow(new DateTimeOffset(2026, 12, 5, 7, 30, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(1);
        time.SetUtcNow(new DateTimeOffset(2026, 12, 25, 7, 30, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(0, "en fazla 3");
        (await ReminderNoticeCountAsync(factory, w.School.AliMother.UserId)).ShouldBe(3);
        pushes.Sent.Count.ShouldBe(6);
    }

    [Fact]
    public async Task SystemDownSeveralDays_OnlyOneReminder_ThenSchedule()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        using OkulApiFactory factory = BillingFactory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        await PlanAsync(client, w.Founder, PlanBody(w.School.Ayse, w.FeeId, installmentCount: 1, firstDueDate: Today));

        time.SetUtcNow(new DateTimeOffset(2026, 11, 29, 8, 0, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(1, "vade+9: birikmiş 1. ve 2.'den yalnızca biri");
        (await ReminderNoticeCountAsync(factory, w.School.AyseMother.UserId)).ShouldBe(1);
        time.SetUtcNow(new DateTimeOffset(2026, 11, 30, 8, 0, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(0);
        time.SetUtcNow(new DateTimeOffset(2026, 12, 5, 8, 0, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(1);
        (await ReminderNoticeCountAsync(factory, w.School.AyseMother.UserId)).ShouldBe(2);
    }

    [Fact]
    public async Task PaidCancelledLeftInactiveGuardianAndOtherGuardians_GetNothing()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        CapturingPushNotifier pushes = new();
        using OkulApiFactory factory = BillingFactory(this.database, pushes, time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid paid = await PlanAsync(client, w.Founder, PlanBody(w.School.Ayse, w.FeeId, installmentCount: 1, firstDueDate: Today));
        await PaidAsync(client, w.Founder, paid, 10_000m);
        Guid cancelled = await PlanAsync(client, w.Founder, PlanBody(w.Cem, w.FeeId, installmentCount: 1, firstDueDate: Today));
        (await CancelPlanAsync(client, w.Founder, cancelled)).Status.ShouldBe(HttpStatusCode.OK);
        await PlanAsync(client, w.Founder, PlanBody(w.School.Berk, w.FeeId, installmentCount: 1, firstDueDate: Today));
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/students/{w.School.Berk}/deactivate", w.Founder)).ShouldBe(HttpStatusCode.OK);
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 1, firstDueDate: Today));
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{w.School.AliFather.UserId}/deactivate", w.Founder)).ShouldBe(HttpStatusCode.OK);

        time.SetUtcNow(new DateTimeOffset(2026, 11, 21, 8, 0, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(1, "yalnızca Ali'nin taksidi");
        pushes.Sent.Select(p => p.RecipientUserId).ShouldBe([w.School.AliMother.UserId]);
        (await ReminderNoticeCountAsync(factory, w.School.AliFather.UserId)).ShouldBe(0, "pasif veli");
        (await ReminderNoticeCountAsync(factory, w.School.AyseMother.UserId)).ShouldBe(0, "ödenmiş taksit / başka öğrencinin velisi");
        (await ReminderNoticeCountAsync(factory, w.CemMother.UserId)).ShouldBe(0, "iptal plan");
        (await ReminderNoticeCountAsync(factory, w.School.BerkMother.UserId)).ShouldBe(0, "ayrılmış öğrenci");
    }

    [Fact]
    public async Task TwoRunsAtOnce_SingleNotice()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        CapturingPushNotifier pushes = new();
        using OkulApiFactory factory = BillingFactory(this.database, pushes, time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 1, firstDueDate: Today));
        await PlanAsync(client, w.Founder, PlanBody(w.School.Ayse, w.FeeId, installmentCount: 1, firstDueDate: Today));
        time.SetUtcNow(new DateTimeOffset(2026, 11, 21, 8, 0, 0, TimeSpan.Zero));

        int[] results = await Task.WhenAll(RunRemindersAsync(factory, w.TenantId), RunRemindersAsync(factory, w.TenantId), RunRemindersAsync(factory, w.TenantId));

        results.Sum().ShouldBe(2, "iki taksit, her biri bir kez");
        (await ReminderNoticeCountAsync(factory, w.School.AliMother.UserId)).ShouldBe(1);
        (await ReminderNoticeCountAsync(factory, w.School.AyseMother.UserId)).ShouldBe(1);
        pushes.Sent.Count.ShouldBe(3, "Ali'nin iki velisi + Ayşe'nin annesi");
    }

    [Fact]
    public async Task GateFails_WarningLogged_OtherRemindersContinue()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        ThrowingPushNotifier failing = new();
        using OkulApiFactory factory = BillingFactory(this.database, failing, time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 1, firstDueDate: Today));
        await PlanAsync(client, w.Founder, PlanBody(w.Cem, w.FeeId, installmentCount: 1, firstDueDate: Today));
        time.SetUtcNow(new DateTimeOffset(2026, 11, 21, 8, 0, 0, TimeSpan.Zero));

        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(2);

        failing.Attempts.ShouldBe(3);
        (await ReminderNoticeCountAsync(factory, w.School.AliMother.UserId)).ShouldBe(1);
        (await ReminderNoticeCountAsync(factory, w.CemMother.UserId)).ShouldBe(1);
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(0, "kapı hatasında hatırlatma gönderilmiş sayılır");
        string log = factory.ReadLog();
        log.Split('\n').Where(l => l.Contains("Bildirim gönderilemedi", StringComparison.Ordinal)).ShouldAllBe(l => l.Contains("[WRN]"));
        log.Split('\n').Count(l => l.Contains("Bildirim gönderilemedi", StringComparison.Ordinal)).ShouldBe(3);
        log.Split('\n').Count(l => l.Contains("Gecikme hatırlatması gönderildi", StringComparison.Ordinal) && l.Contains("[INF]")).ShouldBe(2);
    }
}
