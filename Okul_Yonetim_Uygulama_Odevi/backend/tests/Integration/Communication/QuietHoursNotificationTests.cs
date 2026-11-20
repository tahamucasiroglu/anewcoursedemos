using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Ports;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-9: öğretmen mesai saatlerini kaydeder/kapatır (başlangıç ≥ bitiş, 15 dk adımı dışı, gün yok alan hatası);
/// mesai içinde (Türkiye saati, seçili gün, başlangıç dahil – bitiş hariç) veli mesajı öğretmene bildirim üretir; mesai dışında
/// mesaj iletilir, okunmamış sayısı artar, bildirim yok ve veli "mesai dışında" bilgisini alır; tanımsızken her mesajda
/// bildirim; öğretmenden veliye mesaj saat ne olursa olsun bildirim üretir. Saat: 2026-11-20 Cuma 12:00 Türkiye.
/// </summary>
[Trait("Category", "Integration")]
public sealed class QuietHoursNotificationTests
{
    private readonly TestDatabase database;

    public QuietHoursNotificationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Set_ValidationErrors_SaveAndDisable()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        string teacher = w.Class.Teacher;

        var (s1, order, _) = await SetQuietAsync(client, teacher, "17:00", "08:00", "Monday");
        var (s2, step, _) = await SetQuietAsync(client, teacher, "08:10", "17:00", "Monday");
        var (s3, noDays, _) = await SetQuietAsync(client, teacher, "08:00", "17:00");
        s1.ShouldBe(HttpStatusCode.BadRequest);
        Messages(order).ShouldContain("Başlangıç saati bitişten önce olmalı");
        s2.ShouldBe(HttpStatusCode.BadRequest);
        Messages(step).ShouldContain("Başlangıç saati SS:DD biçiminde ve 15 dakikalık adımlarla olmalı");
        s3.ShouldBe(HttpStatusCode.BadRequest);
        Messages(noDays).ShouldContain("En az bir gün seçilmelidir");
        (await CallAsync(client, HttpMethod.Get, "/api/me/quiet-hours", teacher)).Body.GetProperty("enabled").GetBoolean().ShouldBeFalse();

        var (saved, view, _) = await SetQuietAsync(client, teacher, "08:30", "16:45", "Friday", "Monday");
        saved.ShouldBe(HttpStatusCode.OK);
        JsonElement read = (await CallAsync(client, HttpMethod.Get, "/api/me/quiet-hours", teacher)).Body;
        read.GetProperty("enabled").GetBoolean().ShouldBeTrue();
        read.GetProperty("start").GetString().ShouldBe("08:30");
        read.GetProperty("end").GetString().ShouldBe("16:45");
        read.GetProperty("days").EnumerateArray().Select(d => d.GetString()).ShouldBe(["Monday", "Friday"]);
        view.GetProperty("start").GetString().ShouldBe("08:30");

        (await CallAsync(client, HttpMethod.Put, "/api/me/quiet-hours", teacher, new { enabled = false })).Status.ShouldBe(HttpStatusCode.OK);
        (await CallAsync(client, HttpMethod.Get, "/api/me/quiet-hours", teacher)).Body.GetProperty("enabled").GetBoolean().ShouldBeFalse();
        (await CallAsync(client, HttpMethod.Get, "/api/me/quiet-hours", w.AliMother.Token)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("08:00", "17:00", "Friday", true)]
    [InlineData("12:00", "13:00", "Friday", true)]
    [InlineData("08:00", "12:00", "Friday", false)]
    [InlineData("08:00", "17:00", "Monday", false)]
    public async Task GuardianMessage_NotifiesOnlyWithinWorkingHours(string start, string end, string day, bool within)
    {
        CapturingPushNotifier push = new();
        using OkulApiFactory factory = Factory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (await SetQuietAsync(client, w.Class.Teacher, start, end, day)).Status.ShouldBe(HttpStatusCode.OK);
        Guid id = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);

        var (status, sent, _) = await SendAsync(client, w.AliMother.Token, id, "Ali yarın gelmeyecek");

        status.ShouldBe(HttpStatusCode.OK);
        sent.GetProperty("outsideWorkingHours").GetBoolean().ShouldBe(!within);
        push.To(w.Class.TeacherId).Length.ShouldBe(within ? 1 : 0);
        (await MeAsync(client, w.Class.Teacher)).GetProperty("unreadMessageCount").GetInt32().ShouldBe(1, "mesaj her durumda iletilir");
        (await MessagesAsync(client, w.Class.Teacher, id)).Body.GetProperty("messages").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task NoQuietHours_EveryGuardianMessageNotifies_TeacherMessageAlwaysNotifies()
    {
        CapturingPushNotifier push = new();
        FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 20, 20, 0, 0, TimeSpan.Zero));
        using OkulApiFactory factory = Factory(this.database, push, time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);

        JsonElement first = (await SendAsync(client, w.AliMother.Token, id, "Gece mesajı")).Body;
        first.GetProperty("outsideWorkingHours").GetBoolean().ShouldBeFalse();
        push.To(w.Class.TeacherId).ShouldHaveSingleItem().Kind.ShouldBe(PushNotificationKind.Message);

        (await SetQuietAsync(client, w.Class.Teacher, "08:00", "17:00", "Friday")).Status.ShouldBe(HttpStatusCode.OK);
        (await SendAsync(client, w.Class.Teacher, id, "Gece 23:00'te cevap")).Body.GetProperty("outsideWorkingHours").GetBoolean().ShouldBeFalse();
        push.To(w.AliMother.UserId).ShouldHaveSingleItem().Kind.ShouldBe(PushNotificationKind.Message);
        (await SendAsync(client, w.AliMother.Token, id, "Teşekkürler")).Body.GetProperty("outsideWorkingHours").GetBoolean().ShouldBeTrue();
        push.To(w.Class.TeacherId).Length.ShouldBe(1, "mesai dışı veli mesajı bildirim üretmez");
    }
}
